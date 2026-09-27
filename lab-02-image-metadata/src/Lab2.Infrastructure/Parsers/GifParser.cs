using System.Buffers.Binary;
using Lab2.Core.Contracts;
using Lab2.Core.Models;

namespace Lab2.Infrastructure.Parsers;

/// <summary>Walks GIF block boundaries while skipping compressed image sub-blocks.</summary>
public sealed class GifParser : IImageParser
{
    public ImageFormat Format => ImageFormat.Gif;

    public bool CanParse(ReadOnlySpan<byte> signature) => signature.Length >= 6 &&
        (signature[..6].SequenceEqual("GIF87a"u8) || signature[..6].SequenceEqual("GIF89a"u8));

    public async ValueTask<ImageMetadata> ParseAsync(Stream stream, string filePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        if (!stream.CanRead || !stream.CanSeek)
            throw new ArgumentException("GIF requires a readable, seekable stream.", nameof(stream));

        var result = new ImageMetadata
        {
            FilePath = filePath, FileSizeBytes = stream.Length, Format = Format,
            Compression = "LZW (GIF)"
        };
        ImageMetadata Corrupt(string reason) => result with
        {
            Status = FileProcessingStatus.Corrupted, ErrorMessage = reason
        };

        stream.Position = 0;
        var header = new byte[13];
        if (!await ParserBytes.ReadAsync(stream, header, cancellationToken) || !CanParse(header))
            return Corrupt("Нет полного заголовка GIF87a/GIF89a.");

        var screenWidth = BinaryPrimitives.ReadUInt16LittleEndian(header.AsSpan(6, 2));
        var screenHeight = BinaryPrimitives.ReadUInt16LittleEndian(header.AsSpan(8, 2));
        var packed = header[10];
        if (screenWidth == 0 || screenHeight == 0)
            return Corrupt("Нулевой размер логического экрана GIF.");
        var hasGlobalPalette = (packed & 0x80) != 0;
        var depth = hasGlobalPalette ? (packed & 7) + 1 : ((packed >> 4) & 7) + 1;
        result = result with
        {
            Width = screenWidth, Height = screenHeight, ColorDepth = depth
        };
        if (hasGlobalPalette && !ParserBytes.Skip(stream, 3L * (1 << depth)))
            return Corrupt("Глобальная палитра GIF обрезана.");

        var byteBuffer = new byte[1];
        var sawImage = false;
        while (stream.Position < stream.Length)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!await ParserBytes.ReadAsync(stream, byteBuffer, cancellationToken))
                return Corrupt("Обрезан блок GIF.");
            switch (byteBuffer[0])
            {
                case 0x3b:
                    return sawImage && stream.Position == stream.Length
                        ? result : Corrupt("Завершающий блок GIF не на конце файла или нет изображения.");

                case 0x2c:
                    var descriptor = new byte[9];
                    if (!await ParserBytes.ReadAsync(stream, descriptor, cancellationToken))
                        return Corrupt("Обрезано описание кадра GIF.");
                    var left = BinaryPrimitives.ReadUInt16LittleEndian(descriptor);
                    var top = BinaryPrimitives.ReadUInt16LittleEndian(descriptor.AsSpan(2));
                    var width = BinaryPrimitives.ReadUInt16LittleEndian(descriptor.AsSpan(4));
                    var height = BinaryPrimitives.ReadUInt16LittleEndian(descriptor.AsSpan(6));
                    if (width == 0 || height == 0 || left + width > screenWidth ||
                        top + height > screenHeight)
                        return Corrupt("Кадр выходит за границы логического экрана GIF.");
                    if ((descriptor[8] & 0x80) != 0)
                    {
                        var localDepth = (descriptor[8] & 7) + 1;
                        if (!ParserBytes.Skip(stream, 3L * (1 << localDepth)))
                            return Corrupt("Локальная палитра GIF обрезана.");
                        result = result with { ColorDepth = Math.Max(result.ColorDepth ?? 0, localDepth) };
                    }
                    if (!await ParserBytes.ReadAsync(stream, byteBuffer, cancellationToken) ||
                        byteBuffer[0] is < 2 or > 8 ||
                        !await SkipSubBlocks(stream, cancellationToken))
                        return Corrupt("Обрезаны или некорректны данные кадра GIF.");
                    sawImage = true;
                    break;

                case 0x21:
                    if (!await ParserBytes.ReadAsync(stream, byteBuffer, cancellationToken))
                        return Corrupt("Обрезана метка расширения GIF.");
                    if (byteBuffer[0] == 0xf9)
                    {
                        var control = new byte[6];
                        if (!await ParserBytes.ReadAsync(stream, control, cancellationToken) ||
                            control[0] != 4 || control[5] != 0)
                            return Corrupt("Некорректное расширение управления кадром GIF.");
                    }
                    else if (!await SkipSubBlocks(stream, cancellationToken))
                        return Corrupt("Обрезано расширение GIF.");
                    break;

                default:
                    return Corrupt("Неизвестный тип блока GIF.");
            }
        }

        return Corrupt("Отсутствует завершающий блок GIF (3B).");
    }

    private static async ValueTask<bool> SkipSubBlocks(Stream stream,
        CancellationToken cancellationToken)
    {
        var size = new byte[1];
        while (await ParserBytes.ReadAsync(stream, size, cancellationToken))
        {
            if (size[0] == 0) return true;
            if (!ParserBytes.Skip(stream, size[0])) return false;
        }
        return false;
    }
}
