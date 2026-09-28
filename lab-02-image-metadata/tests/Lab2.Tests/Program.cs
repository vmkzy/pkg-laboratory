using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Compression;
using Lab2.Core.Contracts;
using Lab2.Core.Models;
using Lab2.Core.Services;
using Lab2.Infrastructure.Parsers;

var failures = new List<string>();
var parser = new BmpParser();

Check(new ImageMetadata { FilePath = @"C:\images\sample.bmp" }.FileName == "sample.bmp",
    "Имя файла извлекается из полного пути");
Check(new ScanProgress(25, 10, 9, 1).Percentage == 40,
    "Прогресс вычисляется из числа обработанных файлов");
Check(parser.CanParse("BM"u8) && !parser.CanParse("PNG"u8) && !parser.CanParse([]),
    "Формат определяется по сигнатуре");

var valid = MakeBmp();
var info = await Parse(valid, "renamed.png");
Check(info.Status == FileProcessingStatus.Valid && info.Format == ImageFormat.Bmp,
    "Расширение не определяет формат");
Check(info.Width == 2 && info.Height == 2 && info.ColorDepth == 24 &&
      info.Compression.StartsWith("BI_RGB") && info.FileSizeBytes == valid.Length,
    "Размеры, глубина, сжатие и размер файла читаются из заголовка");
Check(info.DpiX is > 95.9 and < 96.1 && info.DpiY is > 95.9 and < 96.1,
    "Пиксели на метр пересчитываются в DPI");

var topDown = MakeBmp();
I32(topDown, 22, -2);
Check((await Parse(topDown)).Height == 2, "Отрицательная высота означает верхнее направление строк");

var unknownDpi = MakeBmp();
I32(unknownDpi, 38, 0);
I32(unknownDpi, 42, 0);
var noDpi = await Parse(unknownDpi);
Check(noDpi.Status == FileProcessingStatus.Valid && noDpi.DpiX is null && noDpi.DpiY is null,
    "Нулевое разрешение означает, что DPI не задан");

var declaredImageSizeZero = MakeBmp();
U32(declaredImageSizeZero, 34, 0);
Check((await Parse(declaredImageSizeZero)).Status == FileProcessingStatus.Valid,
    "Нулевой biSizeImage допустим для BI_RGB");

var truncated = valid[..^1];
Check((await Parse(truncated)).Status == FileProcessingStatus.Corrupted,
    "Усечённые пиксельные данные обнаруживаются");
Check((await Parse(valid[..13])).Status == FileProcessingStatus.Corrupted,
    "Файл короче заголовка BMP обнаруживается");
Check((await Parse(valid[..18])).Status == FileProcessingStatus.Corrupted,
    "Обрезанный заголовок DIB обнаруживается");

var incorrectOffset = MakeBmp();
U32(incorrectOffset, 10, 20);
Check((await Parse(incorrectOffset)).Status == FileProcessingStatus.Corrupted,
    "Смещение в заголовок обнаруживается");

var incorrectDepth = MakeBmp();
U16(incorrectDepth, 28, 0);
Check((await Parse(incorrectDepth)).Status == FileProcessingStatus.Corrupted,
    "Некорректная глубина обнаруживается");

var wrongSignature = MakeBmp();
wrongSignature[0] = (byte)'X';
Check((await Parse(wrongSignature)).Status == FileProcessingStatus.Corrupted,
    "Отсутствие сигнатуры обнаруживается");

var unsupportedCompression = MakeBmp();
U32(unsupportedCompression, 30, 42);
Check((await Parse(unsupportedCompression)).Status == FileProcessingStatus.Unsupported,
    "Неизвестный код сжатия не выдаётся за корректный файл");

var bitfields = MakeBmp();
U32(bitfields, 30, 3);
Check((await Parse(bitfields)).Status == FileProcessingStatus.Corrupted,
    "Маски BI_BITFIELDS требуют 16 или 32 бит и места после заголовка");

var palette = MakeBmp(2, 2, 8);
Check((await Parse(palette)).Status == FileProcessingStatus.Valid,
    "8-битное изображение с таблицей цветов читается");

var incompletePalette = palette.ToArray();
U32(incompletePalette, 10, 54);
Check((await Parse(incompletePalette)).Status == FileProcessingStatus.Corrupted,
    "Смещение до конца палитры обнаруживается");

var coreHeader = MakeCoreBmp();
var coreInfo = await Parse(coreHeader);
Check(coreInfo.Status == FileProcessingStatus.Valid && coreInfo.Width == 2 &&
      coreInfo.Height == 2 && coreInfo.ColorDepth == 24 && coreInfo.DpiX is null,
    "Короткий заголовок BITMAPCOREHEADER читается без выдуманного DPI");

var v4Header = MakeBmpWithExtendedHeader(108);
Check((await Parse(v4Header)).Status == FileProcessingStatus.Valid,
    "Расширенный заголовок V4 читается без загрузки цветового профиля");

var masks = MakeBitfieldsBmp();
Check((await Parse(masks)).Status == FileProcessingStatus.Valid,
    "Маски BI_BITFIELDS размещены после 40-байтного заголовка");
var missingMasks = masks.ToArray();
U32(missingMasks, 10, 54);
Check((await Parse(missingMasks)).Status == FileProcessingStatus.Corrupted,
    "Отсутствие места под маски BI_BITFIELDS обнаруживается");

var inconsistentImageSize = MakeBmp();
U32(inconsistentImageSize, 34, 1);
Check((await Parse(inconsistentImageSize)).Status == FileProcessingStatus.Corrupted,
    "Ненулевой размер несжатого изображения должен совпадать с вычисленным");

var hugeDimensions = MakeBmp();
I32(hugeDimensions, 18, int.MaxValue);
I32(hugeDimensions, 22, int.MaxValue);
Check((await Parse(hugeDimensions)).Status == FileProcessingStatus.Corrupted,
    "Огромные размеры не приводят к переполнению расчёта пикселей");

await FormatParserTests.RunAsync(Check);
await TiffPcxTests.RunAsync(Check);
await CheckFolderScanner();

if (args.Length == 2 && args[0] == "--samples")
{
    using var archive = ZipFile.OpenRead(args[1]);
    var count = 0;
    foreach (var entry in archive.Entries.Where(e => e.Name.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase)))
    {
        await using var input = entry.Open();
        await using var buffer = new MemoryStream();
        await input.CopyToAsync(buffer);
        buffer.Position = 0;
        var sample = await parser.ParseAsync(buffer, entry.Name);
        Check(sample.Status == FileProcessingStatus.Valid && sample.Width > 0 &&
              sample.Height > 0 && sample.ColorDepth == 8 && sample.DpiX is null,
            $"Проверочный BMP: {entry.Name} ({sample.ErrorMessage})");
        count++;
    }
    Check(count > 0, "В архиве должны быть BMP-файлы");
    Console.WriteLine($"Проверено BMP из архива: {count}.");
}
else if (args.Length == 2 && args[0] == "--probe-image")
{
    await using var input = File.OpenRead(args[1]);
    var signature = new byte[16];
    var read = await input.ReadAsync(signature);
    IImageParser[] parsers = [new BmpParser(), new PngParser(), new JpegParser(),
        new GifParser(), new TiffParser(), new PcxParser()];
    var selected = parsers.FirstOrDefault(item => item.CanParse(signature.AsSpan(0, read)));
    Check(selected is not null, "Формат проверочного изображения распознан");
    if (selected is not null)
    {
        var image = await selected.ParseAsync(input, args[1]);
        Console.WriteLine($"Реальный файл: {image.Format}, {image.Width} × {image.Height}, " +
                          $"{image.ColorDepth} бит, статус {image.Status}.");
        Check(image.Status == FileProcessingStatus.Valid, image.ErrorMessage ??
            "Реальный файл должен быть корректным");
    }
}
else if (args.Length is 1 or 2 && args[0] == "--stress")
{
    var count = args.Length == 2 && int.TryParse(args[1], out var requested)
        ? requested : 100_000;
    if (count is < 1 or > 100_000)
    {
        Console.Error.WriteLine("Для --stress укажите количество от 1 до 100000.");
        return 2;
    }

    await CheckStress(count);
}
else if (args.Length != 0)
{
    Console.Error.WriteLine("Использование: Lab2.Tests [--samples путь-к-zip | --probe-image путь-к-файлу | --stress [количество]]");
    return 2;
}

foreach (var failure in failures)
    Console.Error.WriteLine($"ОШИБКА: {failure}");

Console.WriteLine($"Результат: {failures.Count} ошибок.");
return failures.Count == 0 ? 0 : 1;

async Task<ImageMetadata> Parse(byte[] bytes, string fileName = "sample.bmp")
{
    await using var input = new MemoryStream(bytes);
    return await parser.ParseAsync(input, fileName);
}

void Check(bool condition, string description)
{
    if (!condition) failures.Add(description);
}

static byte[] MakeBmp(int width = 2, int height = 2, int depth = 24)
{
    var paletteLength = depth <= 8 ? (1 << depth) * 4 : 0;
    var pixelOffset = 54 + paletteLength;
    var imageSize = ((width * depth + 31) / 32 * 4) * height;
    var bytes = new byte[pixelOffset + imageSize];
    bytes[0] = (byte)'B';
    bytes[1] = (byte)'M';
    U32(bytes, 2, (uint)bytes.Length);
    U32(bytes, 10, (uint)pixelOffset);
    U32(bytes, 14, 40);
    I32(bytes, 18, width);
    I32(bytes, 22, height);
    U16(bytes, 26, 1);
    U16(bytes, 28, (ushort)depth);
    U32(bytes, 34, (uint)imageSize);
    I32(bytes, 38, 3780);
    I32(bytes, 42, 3780);
    return bytes;
}

static byte[] MakeCoreBmp()
{
    var bytes = new byte[14 + 12 + 16];
    bytes[0] = (byte)'B';
    bytes[1] = (byte)'M';
    U32(bytes, 2, (uint)bytes.Length);
    U32(bytes, 10, 26);
    U32(bytes, 14, 12);
    U16(bytes, 18, 2);
    U16(bytes, 20, 2);
    U16(bytes, 22, 1);
    U16(bytes, 24, 24);
    return bytes;
}

static byte[] MakeBmpWithExtendedHeader(int headerSize)
{
    var original = MakeBmp();
    var bytes = new byte[original.Length + headerSize - 40];
    original.AsSpan(0, 54).CopyTo(bytes);
    U32(bytes, 2, (uint)bytes.Length);
    U32(bytes, 10, (uint)(14 + headerSize));
    U32(bytes, 14, (uint)headerSize);
    return bytes;
}

static byte[] MakeBitfieldsBmp()
{
    var original = MakeBmp(depth: 16);
    var bytes = new byte[original.Length + 12];
    original.AsSpan(0, 54).CopyTo(bytes);
    U32(bytes, 2, (uint)bytes.Length);
    U32(bytes, 10, 66);
    U32(bytes, 30, 3);
    U32(bytes, 54, 0x7c00);
    U32(bytes, 58, 0x03e0);
    U32(bytes, 62, 0x001f);
    return bytes;
}

static void U16(byte[] bytes, int offset, ushort value) =>
    BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(offset), value);

static void U32(byte[] bytes, int offset, uint value) =>
    BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset), value);

static void I32(byte[] bytes, int offset, int value) =>
    BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(offset), value);

async Task CheckFolderScanner()
{
    // The test owns this uniquely created directory and removes only these fixtures.
    var directory = Directory.CreateTempSubdirectory("lab2-scanner-");
    try
    {
        var nested = Directory.CreateDirectory(Path.Combine(directory.FullName, "inside"));
        await File.WriteAllBytesAsync(Path.Combine(directory.FullName, "renamed.png"), MakeBmp());
        await File.WriteAllBytesAsync(Path.Combine(directory.FullName, "broken.bmp"), MakeBmp()[..^1]);
        await File.WriteAllTextAsync(Path.Combine(directory.FullName, "notes.txt"), "not an image");
        await File.WriteAllBytesAsync(Path.Combine(nested.FullName, "nested.bmp"), MakeBmp());

        var scanner = new FolderScanner([new BmpParser()]);
        var reports = new ConcurrentQueue<ScanProgress>();
        var progress = new ImmediateProgress<ScanProgress>(reports.Enqueue);
        var found = new List<ImageMetadata>();
        await foreach (var item in scanner.ScanAsync(new ScanRequest
        {
            FolderPath = directory.FullName,
            WorkerCount = 2,
            IncludeSubdirectories = true
        }, progress))
            found.Add(item);

        Check(found.Count == 4, "Рекурсивный обход возвращает все четыре файла");
        Check(found.Single(item => item.FileName == "renamed.png").Format == ImageFormat.Bmp,
            "Менеджер выбирает парсер по сигнатуре, а не расширению");
        Check(found.Single(item => item.FileName == "broken.bmp").Status == FileProcessingStatus.Corrupted,
            "Повреждённый файл не останавливает соседние файлы");
        Check(found.Single(item => item.FileName == "notes.txt").Status == FileProcessingStatus.Unsupported,
            "Неизвестный формат отображается как неподдерживаемый");
        Check(reports.LastOrDefault() is { DiscoveredFiles: 4, ProcessedFiles: 4,
            ValidFiles: 2, ProblemFiles: 2, EnumerationCompleted: true, Percentage: 100 },
            "Итоговый прогресс содержит верные счётчики");

        found.Clear();
        await foreach (var item in scanner.ScanAsync(new ScanRequest
        {
            FolderPath = directory.FullName,
            WorkerCount = 1,
            IncludeSubdirectories = false
        }))
            found.Add(item);
        Check(found.Count == 3, "При выключенной рекурсии вложенный файл не читается");

        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        try
        {
            await foreach (var item in scanner.ScanAsync(new ScanRequest
            {
                FolderPath = directory.FullName,
                WorkerCount = 2
            }, cancellationToken: canceled.Token))
            {
                _ = item;
            }
            Check(false, "Отменённое сканирование должно прерваться");
        }
        catch (OperationCanceledException)
        {
            // Expected: no worker may keep the scan alive after cancellation.
        }

        for (var index = 0; index < 260; index++)
            await File.WriteAllBytesAsync(Path.Combine(directory.FullName, $"batch-{index:D3}.bmp"), MakeBmp());

        var batchCount = 0;
        await foreach (var item in scanner.ScanAsync(new ScanRequest
        {
            FolderPath = directory.FullName,
            WorkerCount = 1
        }))
        {
            if (item.Status == FileProcessingStatus.Valid) batchCount++;
        }
        Check(batchCount == 262, "Ограниченные очереди пропускают больше файлов, чем их ёмкость");

        await foreach (var item in scanner.ScanAsync(new ScanRequest
        {
            FolderPath = directory.FullName,
            WorkerCount = 1
        }))
        {
            _ = item;
            break;
        }
    }
    finally
    {
        Directory.Delete(directory.FullName, recursive: true);
    }
}

async Task CheckStress(int count)
{
    var directory = Directory.CreateTempSubdirectory("lab2-stress-");
    try
    {
        const int filesPerFolder = 1_000;
        var folderCount = (count + filesPerFolder - 1) / filesPerFolder;
        var folders = new string[folderCount];
        for (var index = 0; index < folderCount; index++)
            folders[index] = Directory.CreateDirectory(
                Path.Combine(directory.FullName, $"part-{index:D3}")).FullName;

        var good = MakeBmp();
        var broken = good[..^1];
        Console.WriteLine($"Создание {count:N0} небольших файлов для нагрузочной проверки...");
        var watch = Stopwatch.StartNew();
        Parallel.For(0, count,
            new ParallelOptions { MaxDegreeOfParallelism = 8 }, index =>
            {
                var extension = index % 50 == 1 ? ".dat" : ".bmp";
                var path = Path.Combine(folders[index / filesPerFolder], $"image-{index:D6}{extension}");
                File.WriteAllBytes(path, index % 20 == 0 ? broken : good);
            });
        Console.WriteLine($"Файлы созданы за {watch.Elapsed.TotalSeconds:0.0} с.");

        var scanner = new FolderScanner([new BmpParser()]);
        ScanProgress? lastProgress = null;
        var progress = new ImmediateProgress<ScanProgress>(value => lastProgress = value);
        var validCount = 0;
        var corruptedCount = 0;
        var renamedCount = 0;
        watch.Restart();
        await foreach (var item in scanner.ScanAsync(new ScanRequest
        {
            FolderPath = directory.FullName,
            WorkerCount = Math.Clamp(Environment.ProcessorCount, 1, 16),
            IncludeSubdirectories = true
        }, progress))
        {
            if (item.Status == FileProcessingStatus.Valid) validCount++;
            if (item.Status == FileProcessingStatus.Corrupted) corruptedCount++;
            if (item.FileName.EndsWith(".dat", StringComparison.OrdinalIgnoreCase) &&
                item.Status == FileProcessingStatus.Valid && item.Format == ImageFormat.Bmp)
                renamedCount++;
        }
        watch.Stop();

        var expectedCorrupted = (count + 19) / 20;
        var expectedRenamed = Enumerable.Range(0, count).Count(index =>
            index % 50 == 1 && index % 20 != 0);
        Check(validCount == count - expectedCorrupted && corruptedCount == expectedCorrupted,
            "Нагрузочный обход возвращает каждый корректный и повреждённый файл один раз");
        Check(renamedCount == expectedRenamed,
            "Нагрузочный обход определяет формат файлов с неверным расширением");
        Check(lastProgress is { EnumerationCompleted: true, Percentage: 100 } &&
              lastProgress.DiscoveredFiles == count && lastProgress.ProcessedFiles == count,
            "Нагрузочный обход завершает прогресс на 100% без потери файлов");
        Console.WriteLine($"Сканирование: {watch.Elapsed.TotalSeconds:0.0} с; " +
            $"корректных: {validCount:N0}; повреждённых: {corruptedCount:N0}; " +
            $"распознано с расширением .dat: {renamedCount:N0}.");
    }
    finally
    {
        // Delete only the unique directory created by this test in the system temp folder.
        var tempRoot = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar);
        var parent = Path.GetFullPath(directory.Parent!.FullName)
            .TrimEnd(Path.DirectorySeparatorChar);
        if (!string.Equals(parent, tempRoot, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Неверный путь временной папки теста.");
        directory.Delete(recursive: true);
    }
}

sealed class ImmediateProgress<T>(Action<T> report) : IProgress<T>
{
    public void Report(T value) => report(value);
}
