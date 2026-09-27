using System.Buffers.Binary;
using Lab2.Core.Contracts;
using Lab2.Core.Models;

namespace Lab2.Infrastructure.Parsers;

/// <summary>Reads PNG chunk headers and metadata without decoding IDAT data.</summary>
public sealed class PngParser : IImageParser
{
    private static ReadOnlySpan<byte> Signature => [137, 80, 78, 71, 13, 10, 26, 10];
    private const double InchesPerMeter = 39.37007874015748;

    public ImageFormat Format => ImageFormat.Png;

    public bool CanParse(ReadOnlySpan<byte> signature) =>
        signature.Length >= 8 && signature[..8].SequenceEqual(Signature);

    public async ValueTask<ImageMetadata> ParseAsync(Stream stream, string filePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        if (!stream.CanRead || !stream.CanSeek)
            throw new ArgumentException("PNG requires a readable, seekable stream.", nameof(stream));

        var result = new ImageMetadata
        {
            FilePath = filePath, FileSizeBytes = stream.Length, Format = Format,
            Compression = "Deflate (PNG)"
        };
        ImageMetadata Corrupt(string reason) => result with
        {
            Status = FileProcessingStatus.Corrupted, ErrorMessage = reason
        };

        stream.Position = 0;
        var signature = new byte[8];
        if (!await ParserBytes.ReadAsync(stream, signature, cancellationToken) || !CanParse(signature))
            return Corrupt("Нет полной сигнатуры PNG.");

        var header = new byte[8];
        var crcBytes = new byte[4];
        var sawHeader = false;
        var sawData = false;
        var sawPalette = false;
        var sawPhysical = false;
        var dataEnded = false;
        long compressedBytes = 0;
        var colorType = -1;
        var sampleDepth = 0;

        while (stream.Position < stream.Length)
        {
            if (!await ParserBytes.ReadAsync(stream, header, cancellationToken))
                return Corrupt("Обрезан заголовок блока PNG.");
            var chunkLength = BinaryPrimitives.ReadUInt32BigEndian(header);
            if (chunkLength > int.MaxValue || chunkLength > stream.Length - stream.Position - 4)
                return Corrupt("Длина блока PNG выходит за пределы файла.");
            var type = header.AsSpan(4, 4).ToArray();
            if (type.IndexOfAnyExceptInRange((byte)'A', (byte)'z') >= 0 ||
                type.ToArray().Any(value => value is > (byte)'Z' and < (byte)'a'))
                return Corrupt("Недопустимое имя блока PNG.");

            if (!sawHeader && !type.SequenceEqual("IHDR"u8))
                return Corrupt("Первым блоком PNG должен быть IHDR.");

            if (type.SequenceEqual("IHDR"u8))
            {
                if (sawHeader || chunkLength != 13)
                    return Corrupt("Повторный или некорректный IHDR.");
                var data = new byte[13];
                if (!await ParserBytes.ReadAsync(stream, data, cancellationToken) ||
                    !await ParserBytes.ReadAsync(stream, crcBytes, cancellationToken) ||
                    !ValidCrc(type, data, crcBytes))
                    return Corrupt("IHDR обрезан или имеет неверную контрольную сумму.");

                var width = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(0, 4));
                var height = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(4, 4));
                var bitDepth = data[8];
                colorType = data[9];
                sampleDepth = bitDepth;
                var channels = colorType switch { 0 => 1, 2 => 3, 3 => 1, 4 => 2, 6 => 4, _ => 0 };
                var allowedDepth = colorType switch
                {
                    0 => bitDepth is 1 or 2 or 4 or 8 or 16,
                    2 or 4 or 6 => bitDepth is 8 or 16,
                    3 => bitDepth is 1 or 2 or 4 or 8,
                    _ => false
                };
                if (width == 0 || height == 0 || width > int.MaxValue || height > int.MaxValue ||
                    !allowedDepth || data[10] != 0 || data[11] != 0 || data[12] > 1)
                    return Corrupt("Некорректные размеры или параметры IHDR.");

                result = result with
                {
                    Width = (int)width, Height = (int)height,
                    ColorDepth = bitDepth * channels,
                    Compression = data[12] == 1 ? "Deflate (PNG), Adam7" : "Deflate (PNG)"
                };
                sawHeader = true;
                continue;
            }

            if (type.SequenceEqual("pHYs"u8))
            {
                if (chunkLength != 9 || sawData || sawPhysical)
                    return Corrupt("Блок pHYs должен предшествовать IDAT и содержать 9 байт.");
                var data = new byte[9];
                if (!await ParserBytes.ReadAsync(stream, data, cancellationToken) ||
                    !await ParserBytes.ReadAsync(stream, crcBytes, cancellationToken) ||
                    !ValidCrc(type, data, crcBytes) || data[8] > 1)
                    return Corrupt("Некорректный блок pHYs.");
                if (data[8] == 1)
                {
                    var x = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(0, 4));
                    var y = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(4, 4));
                    result = result with
                    {
                        DpiX = x > 0 ? x / InchesPerMeter : null,
                        DpiY = y > 0 ? y / InchesPerMeter : null
                    };
                }
                sawPhysical = true;
                continue;
            }

            if (type.SequenceEqual("PLTE"u8))
            {
                if (sawData || sawPalette || colorType is 0 or 4 || chunkLength == 0 ||
                    chunkLength > 768 || chunkLength % 3 != 0 ||
                    colorType == 3 && chunkLength / 3 > 1u << sampleDepth)
                    return Corrupt("Некорректная палитра PNG.");
                sawPalette = true;
                var palette = new byte[(int)chunkLength];
                if (!await ParserBytes.ReadAsync(stream, palette, cancellationToken) ||
                    !await ParserBytes.ReadAsync(stream, crcBytes, cancellationToken) ||
                    !ValidCrc(type, palette, crcBytes))
                    return Corrupt("Палитра PNG обрезана или имеет неверную контрольную сумму.");
                continue;
            }
            else if (type.SequenceEqual("IDAT"u8))
            {
                if (dataEnded || colorType == 3 && !sawPalette)
                    return Corrupt("Некорректный порядок блоков IDAT.");
                sawData = true;
                compressedBytes += chunkLength;
            }
            else if (type.SequenceEqual("IEND"u8))
            {
                if (!sawData || compressedBytes == 0 || chunkLength != 0 ||
                    !await ParserBytes.ReadAsync(stream, crcBytes,
                        cancellationToken) || !ValidCrc(type, [], crcBytes) || stream.Position != stream.Length)
                    return Corrupt("IEND отсутствует, повреждён или не является последним блоком.");
                return result;
            }
            else
            {
                if (sawData) dataEnded = true;
                if (type[0] is >= (byte)'A' and <= (byte)'Z')
                    return result with
                    {
                        Status = FileProcessingStatus.Unsupported,
                        ErrorMessage = "PNG содержит неизвестный обязательный блок."
                    };
            }

            if (!ParserBytes.Skip(stream, chunkLength) ||
                !await ParserBytes.ReadAsync(stream, crcBytes, cancellationToken))
                return Corrupt("Блок PNG обрезан.");
        }

        return Corrupt("Отсутствует завершающий блок IEND.");
    }

    private static bool ValidCrc(ReadOnlySpan<byte> type, ReadOnlySpan<byte> data,
        ReadOnlySpan<byte> expected)
    {
        uint crc = 0xffffffff;
        foreach (var value in type) crc = Update(crc, value);
        foreach (var value in data) crc = Update(crc, value);
        return ~crc == BinaryPrimitives.ReadUInt32BigEndian(expected);
    }

    private static uint Update(uint crc, byte value)
    {
        crc ^= value;
        for (var bit = 0; bit < 8; bit++)
            crc = (crc >> 1) ^ ((crc & 1) == 0 ? 0u : 0xedb88320u);
        return crc;
    }
}
