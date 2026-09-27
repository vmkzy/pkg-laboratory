using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Lab2.Core.Contracts;
using Lab2.Core.Models;

namespace Lab2.Core.Services;

/// <summary>
/// Enumerates paths lazily and processes them with a bounded pool of workers.
/// Both channels have backpressure, so pending files do not grow with folder size.
/// </summary>
public sealed class FolderScanner : IFolderScanner
{
    private readonly IImageParser[] _parsers;

    public FolderScanner(IEnumerable<IImageParser> parsers)
    {
        ArgumentNullException.ThrowIfNull(parsers);
        _parsers = parsers.ToArray();
    }

    public async IAsyncEnumerable<ImageMetadata> ScanAsync(
        ScanRequest request,
        IProgress<ScanProgress>? progress = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.FolderPath) || !Directory.Exists(request.FolderPath))
            throw new DirectoryNotFoundException($"Папка не найдена: {request.FolderPath}");
        if (request.WorkerCount is < 1 or > 64)
            throw new ArgumentOutOfRangeException(nameof(request), "Количество потоков должно быть от 1 до 64.");

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var token = linked.Token;
        var capacity = request.WorkerCount * 4;
        var paths = Channel.CreateBounded<string>(new BoundedChannelOptions(capacity)
        {
            SingleWriter = true,
            SingleReader = false,
            FullMode = BoundedChannelFullMode.Wait
        });
        var results = Channel.CreateBounded<ImageMetadata>(new BoundedChannelOptions(capacity)
        {
            SingleWriter = false,
            SingleReader = true,
            FullMode = BoundedChannelFullMode.Wait
        });

        var discovered = 0;
        var processed = 0;
        var valid = 0;
        var problems = 0;
        var enumerationCompleted = 0;

        void Report() => progress?.Report(new ScanProgress(
            Volatile.Read(ref discovered),
            Volatile.Read(ref processed),
            Volatile.Read(ref valid),
            Volatile.Read(ref problems),
            Volatile.Read(ref enumerationCompleted) != 0));

        async Task ProduceAsync()
        {
            try
            {
                var options = new EnumerationOptions
                {
                    RecurseSubdirectories = request.IncludeSubdirectories,
                    IgnoreInaccessible = true,
                    AttributesToSkip = FileAttributes.ReparsePoint
                };
                foreach (var path in Directory.EnumerateFiles(request.FolderPath, "*", options))
                {
                    token.ThrowIfCancellationRequested();
                    await paths.Writer.WriteAsync(path, token);
                    if (Interlocked.Increment(ref discovered) % 128 == 0) Report();
                }
                Volatile.Write(ref enumerationCompleted, 1);
                Report();
            }
            catch
            {
                linked.Cancel();
                throw;
            }
            finally
            {
                paths.Writer.TryComplete();
            }
        }

        async Task ConsumeAsync()
        {
            try
            {
                await foreach (var path in paths.Reader.ReadAllAsync(token))
                {
                    var metadata = await ReadFileAsync(path, token);
                    await results.Writer.WriteAsync(metadata, token);
                    if (metadata.Status == FileProcessingStatus.Valid)
                        Interlocked.Increment(ref valid);
                    else
                        Interlocked.Increment(ref problems);
                    if (Interlocked.Increment(ref processed) % 128 == 0) Report();
                }
            }
            catch
            {
                linked.Cancel();
                throw;
            }
        }

        // Enumeration is moved off the UI thread; file I/O uses async FileStream.
        var producer = Task.Run(ProduceAsync, token);
        var workers = Enumerable.Range(0, request.WorkerCount)
            .Select(_ => Task.Run(ConsumeAsync, token)).ToArray();
        var completion = CompleteAsync();

        async Task CompleteAsync()
        {
            try
            {
                await Task.WhenAll(workers.Append(producer));
                Report();
                results.Writer.TryComplete();
            }
            catch (Exception error)
            {
                linked.Cancel();
                results.Writer.TryComplete(error);
            }
        }

        try
        {
            await foreach (var item in results.Reader.ReadAllAsync(cancellationToken))
                yield return item;
        }
        finally
        {
            linked.Cancel();
            await completion;
        }
    }

    private async Task<ImageMetadata> ReadFileAsync(string path, CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete, 4096,
                FileOptions.Asynchronous | FileOptions.SequentialScan);

            var signature = new byte[16];
            var length = 0;
            while (length < signature.Length)
            {
                var read = await stream.ReadAsync(signature.AsMemory(length), cancellationToken);
                if (read == 0) break;
                length += read;
            }

            var parser = _parsers.FirstOrDefault(item => item.CanParse(signature.AsSpan(0, length)));
            if (parser is null)
            {
                return new ImageMetadata
                {
                    FilePath = path,
                    FileSizeBytes = stream.Length,
                    Status = FileProcessingStatus.Unsupported,
                    ErrorMessage = "Формат по сигнатуре пока не поддерживается."
                };
            }

            stream.Position = 0;
            return await parser.ParseAsync(stream, path, cancellationToken);
        }
        catch (Exception error) when (error is not (OperationCanceledException or OutOfMemoryException))
        {
            return new ImageMetadata
            {
                FilePath = path,
                Status = FileProcessingStatus.Failed,
                ErrorMessage = error.Message
            };
        }
    }
}
