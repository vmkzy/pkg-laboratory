using System.Buffers.Binary;
using Lab2.Core.Contracts;
using Lab2.Core.Models;
using Lab2.Core.Services;
using Lab2.Infrastructure.Parsers;

internal static class TiffPcxTests
{
    public static async Task RunAsync(Action<bool, string> check)
    {
        var tiffParser = new TiffParser();
        var pcxParser = new PcxParser();

        var little = MakeTiff(littleEndian: true);
        var littleInfo = await Parse(tiffParser, little);
        check(tiffParser.CanParse(little) && littleInfo.Status == FileProcessingStatus.Valid &&
              littleInfo.Width == 3 && littleInfo.Height == 2 && littleInfo.ColorDepth == 24 &&
              littleInfo.DpiX == 300 && littleInfo.DpiY == 150 &&
              littleInfo.Compression == "Без сжатия",
            "TIFF II: IFD, внешние BitsPerSample и RATIONAL читаются по смещениям");

        var big = MakeTiff(littleEndian: false);
        var bigInfo = await Parse(tiffParser, big);
        check(tiffParser.CanParse(big) && bigInfo.Status == FileProcessingStatus.Valid &&
              bigInfo.Width == 3 && bigInfo.ColorDepth == 24 && bigInfo.DpiX == 300,
            "TIFF MM: порядок байтов применяется ко всем тегам и дробям");
        check((await Parse(tiffParser, little[..^1])).Status == FileProcessingStatus.Corrupted,
            "Обрезанная последняя полоса TIFF обнаруживается");
        var wrongIfd = little.ToArray();
        Put32(wrongIfd, 4, 999999, true);
        check((await Parse(tiffParser, wrongIfd)).Status == FileProcessingStatus.Corrupted,
            "Указатель IFD за пределами файла обнаруживается");
        var zeroDenominator = little.ToArray();
        Put32(zeroDenominator, 156, 0, true);
        check((await Parse(tiffParser, zeroDenominator)).Status == FileProcessingStatus.Corrupted,
            "Нулевой знаменатель TIFF RATIONAL обнаруживается");
        var centimeters = little.ToArray();
        Put16(centimeters, 138, 3, true);
        var centimeterInfo = await Parse(tiffParser, centimeters);
        check(centimeterInfo.Status == FileProcessingStatus.Valid &&
              Math.Abs((centimeterInfo.DpiX ?? 0) - 762) < 0.001,
            "TIFF ResolutionUnit=3 пересчитывает пиксели/см в DPI");
        var multipleStrips = MakeMultiStripTiff();
        check((await Parse(tiffParser, multipleStrips)).Status == FileProcessingStatus.Valid,
            "Несколько смещений полос TIFF читаются из внешнего массива IFD");
        var brokenSecondStrip = multipleStrips.ToArray();
        Put32(brokenSecondStrip, 190, 201, true);
        check((await Parse(tiffParser, brokenSecondStrip)).Status == FileProcessingStatus.Corrupted,
            "Выход второй полосы TIFF за пределы файла обнаруживается");
        var tiled = little.ToArray();
        Put16(tiled, 70, 324, true);
        Put16(tiled, 94, 325, true);
        check((await Parse(tiffParser, tiled)).Status == FileProcessingStatus.Valid,
            "Плиточный TIFF использует TileOffsets и TileByteCounts");
        var bigTiff = new byte[16];
        bigTiff[0] = (byte)'I'; bigTiff[1] = (byte)'I'; bigTiff[2] = 43;
        check((await Parse(tiffParser, bigTiff)).Status == FileProcessingStatus.Unsupported,
            "BigTIFF помечается как неподдерживаемый, а не принимается за обычный TIFF");

        var indexed = MakePcx(indexed: true);
        var indexedInfo = await Parse(pcxParser, indexed);
        check(pcxParser.CanParse(indexed) && indexedInfo.Status == FileProcessingStatus.Valid &&
              indexedInfo.Width == 2 && indexedInfo.Height == 1 &&
              indexedInfo.ColorDepth == 8 && indexedInfo.DpiX == 96 && indexedInfo.DpiY == 72,
            "PCX: координаты, плоскости, DPI и хвостовая палитра читаются вручную");
        indexed[131] = 10; indexed[132] = 20; indexed[133] = 30;
        indexed[134] = 40; indexed[135] = 50; indexed[136] = 60;
        var indexedPreview = PcxPreviewDecoder.Decode(new MemoryStream(indexed));
        check(indexedPreview.Width == 2 && indexedPreview.Height == 1 &&
              indexedPreview.BgrPixels.SequenceEqual(new byte[] { 30, 20, 10, 60, 50, 40 }),
            "Предпросмотр 8-битного PCX использует палитру");
        var rgb = MakePcx(indexed: false);
        var rgbInfo = await Parse(pcxParser, rgb);
        check(rgbInfo.Status == FileProcessingStatus.Valid && rgbInfo.ColorDepth == 24,
            "Три 8-битные плоскости PCX дают глубину 24 бита");
        var rgbPreview = PcxPreviewDecoder.Decode(new MemoryStream(rgb));
        check(rgbPreview.BgrPixels.SequenceEqual(new byte[] { 4, 2, 0, 5, 3, 1 }),
            "Предпросмотр 24-битного PCX собирает цвет из трёх плоскостей");
        var compressed = indexed.ToArray();
        compressed[128] = 0xc2; compressed[129] = 7;
        compressed[131 + 7 * 3] = 11;
        compressed[132 + 7 * 3] = 22;
        compressed[133 + 7 * 3] = 33;
        check(PcxPreviewDecoder.Decode(new MemoryStream(compressed)).BgrPixels
                .SequenceEqual(new byte[] { 33, 22, 11, 33, 22, 11 }),
            "Предпросмотр PCX разворачивает RLE-серии");
        var missingPalette = indexed.ToArray();
        missingPalette[130] = 0;
        check((await Parse(pcxParser, missingPalette)).Status == FileProcessingStatus.Corrupted,
            "Отсутствие маркера палитры PCX обнаруживается");
        check((await Parse(pcxParser, rgb[..129])).Status == FileProcessingStatus.Corrupted,
            "Слишком короткие данные PCX обнаруживаются");
        var invalidLine = rgb.ToArray();
        Put16(invalidLine, 66, 1, true);
        check((await Parse(pcxParser, invalidLine)).Status == FileProcessingStatus.Corrupted,
            "Неверная длина строки PCX обнаруживается");

        var directory = Directory.CreateTempSubdirectory("lab2-tiff-pcx-");
        try
        {
            await File.WriteAllBytesAsync(Path.Combine(directory.FullName, "little.tif"), little);
            await File.WriteAllBytesAsync(Path.Combine(directory.FullName, "big.tiff"), big);
            await File.WriteAllBytesAsync(Path.Combine(directory.FullName, "indexed.pcx"), indexed);
            await File.WriteAllBytesAsync(Path.Combine(directory.FullName, "wrong.png"), rgb);
            await File.WriteAllBytesAsync(Path.Combine(directory.FullName, "broken.tif"), little[..^1]);
            var scanner = new FolderScanner([tiffParser, pcxParser]);
            var found = new List<ImageMetadata>();
            await foreach (var item in scanner.ScanAsync(new ScanRequest
            {
                FolderPath = directory.FullName, WorkerCount = 2
            })) found.Add(item);
            check(found.Count == 5 && found.Count(item => item.Status == FileProcessingStatus.Valid) == 4,
                "Общий сканер обрабатывает TIFF и PCX без остановки на повреждённом файле");
            check(found.Single(item => item.FileName == "wrong.png").Format == ImageFormat.Pcx,
                "PCX определяется по содержимому, а не расширению");
        }
        finally
        {
            Directory.Delete(directory.FullName, recursive: true);
        }
    }

    private static async Task<ImageMetadata> Parse(IImageParser parser, byte[] bytes)
    {
        await using var stream = new MemoryStream(bytes);
        return await parser.ParseAsync(stream, "sample.bin");
    }

    private static byte[] MakeTiff(bool littleEndian)
    {
        // Header -> IFD at byte 20 -> three out-of-line tag values -> one 18-byte strip.
        var bytes = new byte[186];
        bytes[0] = bytes[1] = littleEndian ? (byte)'I' : (byte)'M';
        Put16(bytes, 2, 42, littleEndian);
        Put32(bytes, 4, 20, littleEndian);
        Put16(bytes, 20, 10, littleEndian);
        var position = 22;
        Entry(256, 3, 1, 3);     // ImageWidth
        Entry(257, 4, 1, 2);     // ImageLength
        Entry(258, 3, 3, 146);   // BitsPerSample: 8,8,8
        Entry(259, 3, 1, 1);     // Compression: none
        Entry(273, 4, 1, 168);   // StripOffsets
        Entry(277, 3, 1, 3);     // SamplesPerPixel
        Entry(279, 4, 1, 18);    // StripByteCounts
        Entry(282, 5, 1, 152);   // XResolution: 300/1
        Entry(283, 5, 1, 160);   // YResolution: 150/1
        Entry(296, 3, 1, 2);     // ResolutionUnit: inch
        Put32(bytes, position, 0, littleEndian); // no next IFD
        Put16(bytes, 146, 8, littleEndian);
        Put16(bytes, 148, 8, littleEndian);
        Put16(bytes, 150, 8, littleEndian);
        Put32(bytes, 152, 300, littleEndian);
        Put32(bytes, 156, 1, littleEndian);
        Put32(bytes, 160, 150, littleEndian);
        Put32(bytes, 164, 1, littleEndian);
        return bytes;

        void Entry(ushort tag, ushort type, uint count, uint value)
        {
            Put16(bytes, position, tag, littleEndian);
            Put16(bytes, position + 2, type, littleEndian);
            Put32(bytes, position + 4, count, littleEndian);
            if (type == 3 && count == 1)
                Put16(bytes, position + 8, (ushort)value, littleEndian);
            else
                Put32(bytes, position + 8, value, littleEndian);
            position += 12;
        }
    }

    private static byte[] MakePcx(bool indexed)
    {
        var pixels = indexed ? new byte[] { 0, 1 } : [0, 1, 2, 3, 4, 5];
        var bytes = new byte[128 + pixels.Length + (indexed ? 769 : 0)];
        bytes[0] = 0x0a;
        bytes[1] = 5;
        bytes[2] = 1; // RLE
        bytes[3] = 8; // bits per plane
        Put16(bytes, 8, 1, true); // xmax = 1, xmin = 0 -> width = 2
        Put16(bytes, 10, 0, true);
        Put16(bytes, 12, 96, true);
        Put16(bytes, 14, 72, true);
        bytes[65] = indexed ? (byte)1 : (byte)3;
        Put16(bytes, 66, 2, true);
        pixels.CopyTo(bytes, 128);
        if (indexed) bytes[128 + pixels.Length] = 0x0c;
        return bytes;
    }

    private static byte[] MakeMultiStripTiff()
    {
        var original = MakeTiff(littleEndian: true);
        var bytes = new byte[202];
        original.CopyTo(bytes, 0);
        Put32(bytes, 74, 2, true); // StripOffsets count
        Put32(bytes, 78, 186, true);
        Put32(bytes, 98, 2, true); // StripByteCounts count
        Put32(bytes, 102, 194, true);
        Put32(bytes, 186, 168, true);
        Put32(bytes, 190, 177, true);
        Put32(bytes, 194, 9, true);
        Put32(bytes, 198, 9, true);
        return bytes;
    }

    private static void Put16(byte[] bytes, int offset, ushort value, bool littleEndian)
    {
        if (littleEndian) BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(offset), value);
        else BinaryPrimitives.WriteUInt16BigEndian(bytes.AsSpan(offset), value);
    }

    private static void Put32(byte[] bytes, int offset, uint value, bool littleEndian)
    {
        if (littleEndian) BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset), value);
        else BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(offset), value);
    }
}
