using System.Buffers.Binary;
using Lab2.Core.Contracts;
using Lab2.Core.Models;

namespace Lab2.Infrastructure.Parsers;

/// <summary>
/// Reads BMP file and DIB headers directly. It does not decode the image pixels.
/// </summary>
public sealed class BmpParser : IImageParser
{
    private const int FileHeaderSize = 14;
    private const double InchesPerMeter = 39.37007874015748;

    public ImageFormat Format => ImageFormat.Bmp;

    public bool CanParse(ReadOnlySpan<byte> signature) =>
        signature.Length >= 2 && signature[0] == (byte)'B' && signature[1] == (byte)'M';

    public async ValueTask<ImageMetadata> ParseAsync(
        Stream stream,
        string filePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        if (!stream.CanRead || !stream.CanSeek)
        {
            throw new ArgumentException("BMP parsing requires a readable, seekable stream.", nameof(stream));
        }

        cancellationToken.ThrowIfCancellationRequested();
        var length = stream.Length;
        var result = new ImageMetadata
        {
            FilePath = filePath,
            FileSizeBytes = length,
            Format = Format
        };

        ImageMetadata Corrupt(string reason) => result with
        {
            Status = FileProcessingStatus.Corrupted,
            ErrorMessage = reason
        };

        // These small fixed buffers are the only bytes held in memory.
        var fileHeader = new byte[FileHeaderSize];
        stream.Seek(0, SeekOrigin.Begin);
        if (!await ReadHeaderAsync(stream, fileHeader, cancellationToken))
            return Corrupt("Файл короче заголовка BMP (14 байт).");

        if (!CanParse(fileHeader))
            return Corrupt("Нет сигнатуры BM.");

        var declaredFileSize = U32(fileHeader, 2);
        var pixelOffset = U32(fileHeader, 10);
        if (U16(fileHeader, 6) != 0 || U16(fileHeader, 8) != 0)
            return Corrupt("Резервные поля BMP должны быть нулевыми.");
        if (declaredFileSize < FileHeaderSize + 4 || declaredFileSize > length)
            return Corrupt("Размер файла в заголовке не соответствует фактическому размеру.");

        var dibSizeBytes = new byte[4];
        if (!await ReadHeaderAsync(stream, dibSizeBytes, cancellationToken))
            return Corrupt("Отсутствует размер заголовка DIB.");

        var dibSize = U32(dibSizeBytes, 0);
        if (dibSize != 12 && dibSize is not (40 or 52 or 56 or 108 or 124))
            return Corrupt($"Неподдерживаемый размер заголовка DIB: {dibSize}.");
        if ((long)FileHeaderSize + dibSize > length || (long)FileHeaderSize + dibSize > declaredFileSize)
            return Corrupt("Заголовок DIB обрезан.");

        // The first 40 bytes suffice for BITMAPINFOHEADER, V2/V3/V4/V5.
        // BITMAPCOREHEADER has only 12 bytes and 3-byte palette entries.
        var prefix = new byte[dibSize == 12 ? 12 : 40];
        dibSizeBytes.CopyTo(prefix, 0);
        if (!await ReadHeaderAsync(stream, prefix.AsMemory(4), cancellationToken))
            return Corrupt("Заголовок DIB обрезан.");

        int width;
        long height;
        int depth;
        uint compression;
        uint imageSize;
        uint colorsUsed;
        long paletteEntrySize;
        double? dpiX = null;
        double? dpiY = null;

        if (dibSize == 12)
        {
            width = U16(prefix, 4);
            height = U16(prefix, 6);
            depth = U16(prefix, 10);
            compression = 0;
            imageSize = 0;
            colorsUsed = 0;
            paletteEntrySize = 3;
            if (U16(prefix, 8) != 1)
                return Corrupt("Количество цветовых плоскостей должно быть равно 1.");
        }
        else
        {
            width = I32(prefix, 4);
            var signedHeight = I32(prefix, 8);
            height = Math.Abs((long)signedHeight);
            depth = U16(prefix, 14);
            compression = U32(prefix, 16);
            imageSize = U32(prefix, 20);
            colorsUsed = U32(prefix, 32);
            paletteEntrySize = 4;
            dpiX = PixelsPerMeterToDpi(I32(prefix, 24));
            dpiY = PixelsPerMeterToDpi(I32(prefix, 28));
            if (U16(prefix, 12) != 1)
                return Corrupt("Количество цветовых плоскостей должно быть равно 1.");
            if (signedHeight == int.MinValue)
                return Corrupt("Некорректная высота изображения.");
            if (signedHeight < 0 && compression is not (0 or 3 or 6))
                return Corrupt("Сжатое BMP не может храниться сверху вниз.");
        }

        result = result with
        {
            Width = width,
            Height = height <= int.MaxValue ? (int)height : null,
            ColorDepth = depth,
            DpiX = dpiX,
            DpiY = dpiY,
            Compression = CompressionName(compression)
        };

        if (width <= 0 || height == 0 || depth is not (1 or 4 or 8 or 16 or 24 or 32))
            return Corrupt("Некорректные размеры или глубина цвета BMP.");
        if (compression > 6)
            return result with
            {
                Status = FileProcessingStatus.Unsupported,
                ErrorMessage = $"Код сжатия BMP {compression} пока не поддерживается."
            };
        if (compression == 1 && depth != 8 || compression == 2 && depth != 4 ||
            compression is (3 or 6) && depth is not (16 or 32))
            return Corrupt("Алгоритм сжатия несовместим с глубиной цвета.");

        var paletteEntries = depth <= 8 ? (colorsUsed == 0 ? 1L << depth : colorsUsed) : colorsUsed;
        if (depth <= 8 && paletteEntries > 1L << depth)
            return Corrupt("Таблица цветов больше допустимого количества оттенков.");

        var trailingMasks = dibSize == 40 && compression is (3 or 6)
            ? compression == 6 ? 16 : 12
            : 0;
        var minimumOffset = FileHeaderSize + (long)dibSize + trailingMasks + paletteEntries * paletteEntrySize;
        if (pixelOffset < minimumOffset || pixelOffset >= declaredFileSize || pixelOffset >= length)
            return Corrupt("Смещение пикселей указывает внутрь заголовка или за пределы файла.");

        if (compression is (0 or 3 or 6))
        {
            // Rows are padded to four bytes; BI_RGB may legitimately set biSizeImage to zero.
            var stride = ((long)width * depth + 31) / 32 * 4;
            var availablePixels = Math.Min((long)declaredFileSize - pixelOffset, length - pixelOffset);
            if (stride > availablePixels / height)
                return Corrupt("Пиксельные данные BMP обрезаны.");
            var requiredPixels = stride * height;
            if (imageSize != 0 && imageSize != requiredPixels)
                return Corrupt("Размер пиксельных данных противоречит размерам изображения.");
        }
        else if (imageSize != 0 && imageSize > declaredFileSize - pixelOffset)
        {
            return Corrupt("Заявленный размер сжатых данных выходит за пределы файла.");
        }

        return result;
    }

    private static async ValueTask<bool> ReadHeaderAsync(
        Stream stream, Memory<byte> bytes, CancellationToken cancellationToken)
    {
        var read = 0;
        while (read < bytes.Length)
        {
            var count = await stream.ReadAsync(bytes[read..], cancellationToken);
            if (count == 0) return false;
            read += count;
        }
        return true;
    }

    private static ushort U16(byte[] bytes, int offset) =>
        BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(offset));

    private static uint U32(byte[] bytes, int offset) =>
        BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(offset));

    private static int I32(byte[] bytes, int offset) =>
        BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(offset));

    private static double? PixelsPerMeterToDpi(int value) =>
        value > 0 ? value / InchesPerMeter : null;

    private static string CompressionName(uint code) => code switch
    {
        0 => "BI_RGB (без сжатия)",
        1 => "BI_RLE8",
        2 => "BI_RLE4",
        3 => "BI_BITFIELDS",
        4 => "BI_JPEG",
        5 => "BI_PNG",
        6 => "BI_ALPHABITFIELDS",
        _ => $"Неизвестный код ({code})"
    };
}
