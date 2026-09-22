using Lab2.Core.Models;

namespace Lab2.Core.Contracts;

/// <summary>
/// Контракт ручного побайтового парсера одного графического формата.
/// </summary>
public interface IImageParser
{
    ImageFormat Format { get; }

    bool CanParse(ReadOnlySpan<byte> signature);

    /// <summary>
    /// Reads metadata from a readable, seekable stream; the caller owns and closes it.
    /// A malformed image returns Corrupted, while I/O failures may throw.
    /// </summary>
    ValueTask<ImageMetadata> ParseAsync(
        Stream stream,
        string filePath,
        CancellationToken cancellationToken = default);
}
