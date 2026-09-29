using System.Buffers.Binary;
using System.IO.Compression;
using Lab2.Core.Models;
using Lab2.Core.Services;
using Lab2.Infrastructure.Parsers;

internal static class FormatParserTests
{
    public static async Task CreateDemoFilesAsync(string directoryPath,
        Action<bool, string> check)
    {
        if (Directory.Exists(directoryPath))
            throw new IOException($"Папка для примеров уже существует: {directoryPath}");

        Directory.CreateDirectory(directoryPath);
        var png = MakePng();
        await File.WriteAllBytesAsync(Path.Combine(directoryPath, "01-correct.png"), png);
        await File.WriteAllBytesAsync(Path.Combine(directoryPath, "02-png-without-iend.png"),
            png[..^12]);
        await File.WriteAllBytesAsync(Path.Combine(directoryPath, "03-png-renamed-to-jpg.jpg"), png);
        await File.WriteAllTextAsync(Path.Combine(directoryPath, "04-not-an-image.jpg"),
            "This text file is not a JPEG image.");

        var scanner = new FolderScanner([new BmpParser(), new PngParser(), new JpegParser(),
            new GifParser(), new TiffParser(), new PcxParser()]);
        var results = new List<ImageMetadata>();
        await foreach (var item in scanner.ScanAsync(new ScanRequest
        {
            FolderPath = directoryPath,
            WorkerCount = 2
        })) results.Add(item);

        check(results.Count == 4, "В папке примеров должны быть четыре файла");
        if (results.Count != 4) return;
        check(results.Single(item => item.FileName == "01-correct.png").Status ==
              FileProcessingStatus.Valid, "Контрольный PNG корректен");
        check(results.Single(item => item.FileName == "02-png-without-iend.png").Status ==
              FileProcessingStatus.Corrupted, "PNG без IEND помечается повреждённым");
        var renamed = results.Single(item => item.FileName == "03-png-renamed-to-jpg.jpg");
        check(renamed.Status == FileProcessingStatus.Valid && renamed.Format == ImageFormat.Png,
            "PNG с расширением JPG распознаётся по сигнатуре");
        check(results.Single(item => item.FileName == "04-not-an-image.jpg").Status ==
              FileProcessingStatus.Unsupported, "Текст с расширением JPG не считается изображением");
    }

    public static async Task RunAsync(Action<bool, string> check)
    {
        var pngParser = new PngParser();
        var jpegParser = new JpegParser();
        var gifParser = new GifParser();

        var png = MakePng();
        var pngInfo = await Parse(pngParser, png, "not-png.txt");
        check(pngParser.CanParse(png) && pngInfo.Status == FileProcessingStatus.Valid &&
              pngInfo.Width == 1 && pngInfo.Height == 1 && pngInfo.ColorDepth == 8 &&
              pngInfo.DpiX is > 95.9 and < 96.1,
            "PNG: сигнатура, IHDR и pHYs читаются вручную");
        check((await Parse(pngParser, png[..^12])).Status == FileProcessingStatus.Corrupted,
            "PNG без IEND считается повреждённым");
        var badPngCrc = png.ToArray();
        badPngCrc[29] ^= 1;
        check((await Parse(pngParser, badPngCrc)).Status == FileProcessingStatus.Corrupted,
            "Неверная контрольная сумма IHDR обнаруживается");

        var jpeg = MakeJpeg();
        var jpegInfo = await Parse(jpegParser, jpeg, "renamed.bmp");
        check(jpegParser.CanParse(jpeg) && jpegInfo.Status == FileProcessingStatus.Valid &&
              jpegInfo.Width == 1 && jpegInfo.Height == 1 && jpegInfo.ColorDepth == 24 &&
              jpegInfo.DpiX == 96,
            "JPEG: SOF и плотность JFIF читаются по маркерам");
        check((await Parse(jpegParser, jpeg[..^2])).Status == FileProcessingStatus.Corrupted,
            "JPEG без EOI считается повреждённым");
        var badJpegLength = jpeg.ToArray();
        badJpegLength[4] = 0xff;
        badJpegLength[5] = 0xff;
        check((await Parse(jpegParser, badJpegLength)).Status == FileProcessingStatus.Corrupted,
            "Выход длины сегмента JPEG за файл обнаруживается");

        var gif = MakeGif();
        var gifInfo = await Parse(gifParser, gif, "not-gif.jpg");
        check(gifParser.CanParse(gif) && gifInfo.Status == FileProcessingStatus.Valid &&
              gifInfo.Width == 1 && gifInfo.Height == 1 && gifInfo.ColorDepth == 1 &&
              gifInfo.DpiX is null,
            "GIF: логический экран, палитра и блок изображения читаются вручную");
        check((await Parse(gifParser, gif[..^1])).Status == FileProcessingStatus.Corrupted,
            "GIF без завершающего блока считается повреждённым");
        var badGif = gif.ToArray();
        badGif[^5] = 200;
        check((await Parse(gifParser, badGif)).Status == FileProcessingStatus.Corrupted,
            "Обрезанный подблок GIF обнаруживается");

        var directory = Directory.CreateTempSubdirectory("lab2-formats-");
        try
        {
            await File.WriteAllBytesAsync(Path.Combine(directory.FullName, "image.png"), png);
            await File.WriteAllBytesAsync(Path.Combine(directory.FullName, "image.jpg"), jpeg);
            await File.WriteAllBytesAsync(Path.Combine(directory.FullName, "image.gif"), gif);
            await File.WriteAllBytesAsync(Path.Combine(directory.FullName, "wrong-name.bmp"), jpeg);
            await File.WriteAllBytesAsync(Path.Combine(directory.FullName, "broken.png"), png[..^12]);

            var scanner = new FolderScanner([new BmpParser(), pngParser, jpegParser, gifParser]);
            var results = new List<ImageMetadata>();
            await foreach (var item in scanner.ScanAsync(new ScanRequest
            {
                FolderPath = directory.FullName, WorkerCount = 3
            })) results.Add(item);

            check(results.Count == 5 && results.Count(item => item.Status == FileProcessingStatus.Valid) == 4,
                "Общий сканер обрабатывает три новых формата и отдельный повреждённый файл");
            check(results.Single(item => item.FileName == "wrong-name.bmp").Format == ImageFormat.Jpeg,
                "Расширение не подменяет сигнатуру JPEG");
        }
        finally
        {
            Directory.Delete(directory.FullName, recursive: true);
        }
    }

    private static async Task<ImageMetadata> Parse(Lab2.Core.Contracts.IImageParser parser,
        byte[] bytes, string name = "sample.img")
    {
        await using var stream = new MemoryStream(bytes);
        return await parser.ParseAsync(stream, name);
    }

    private static byte[] MakePng()
    {
        using var output = new MemoryStream();
        output.Write([137, 80, 78, 71, 13, 10, 26, 10]);
        var ihdr = new byte[13];
        BinaryPrimitives.WriteUInt32BigEndian(ihdr.AsSpan(0, 4), 1);
        BinaryPrimitives.WriteUInt32BigEndian(ihdr.AsSpan(4, 4), 1);
        ihdr[8] = 8; // eight-bit grayscale, one pixel
        WritePngChunk(output, "IHDR"u8, ihdr);
        var physical = new byte[9];
        BinaryPrimitives.WriteUInt32BigEndian(physical.AsSpan(0, 4), 3780);
        BinaryPrimitives.WriteUInt32BigEndian(physical.AsSpan(4, 4), 3780);
        physical[8] = 1;
        WritePngChunk(output, "pHYs"u8, physical);
        using var packed = new MemoryStream();
        using (var zlib = new ZLibStream(packed, CompressionLevel.Optimal, leaveOpen: true))
            zlib.Write([0, 0]); // filter byte and grayscale pixel
        WritePngChunk(output, "IDAT"u8, packed.ToArray());
        WritePngChunk(output, "IEND"u8, []);
        return output.ToArray();
    }

    private static void WritePngChunk(Stream output, ReadOnlySpan<byte> type,
        ReadOnlySpan<byte> data)
    {
        Span<byte> number = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(number, (uint)data.Length);
        output.Write(number);
        output.Write(type);
        output.Write(data);
        uint crc = 0xffffffff;
        foreach (var value in type) crc = CrcByte(crc, value);
        foreach (var value in data) crc = CrcByte(crc, value);
        BinaryPrimitives.WriteUInt32BigEndian(number, ~crc);
        output.Write(number);
    }

    private static uint CrcByte(uint crc, byte value)
    {
        crc ^= value;
        for (var bit = 0; bit < 8; bit++)
            crc = (crc >> 1) ^ ((crc & 1) == 0 ? 0u : 0xedb88320u);
        return crc;
    }

    private static byte[] MakeJpeg() =>
    [
        0xff, 0xd8, // SOI
        0xff, 0xe0, 0x00, 0x10, (byte)'J', (byte)'F', (byte)'I', (byte)'F', 0,
        1, 2, 1, 0, 96, 0, 96, 0, 0, // APP0 JFIF, 96 dpi
        0xff, 0xc0, 0, 17, 8, 0, 1, 0, 1, 3,
        1, 0x11, 0, 2, 0x11, 1, 3, 0x11, 1, // SOF0: 1x1, 3x8 bit
        0xff, 0xda, 0, 12, 3, 1, 0, 2, 0x11, 3, 0x11, 0, 0x3f, 0,
        0, 0xff, 0xd9 // SOS, placeholder entropy, EOI
    ];

    private static byte[] MakeGif() =>
    [
        (byte)'G', (byte)'I', (byte)'F', (byte)'8', (byte)'9', (byte)'a',
        1, 0, 1, 0, 0x80, 0, 0, // screen 1x1, two palette entries
        0, 0, 0, 255, 255, 255,
        0x2c, 0, 0, 0, 0, 1, 0, 1, 0, 0, // image descriptor
        2, 2, 0x44, 0x01, 0, 0x3b // LZW minimum, data, terminator, trailer
    ];
}
