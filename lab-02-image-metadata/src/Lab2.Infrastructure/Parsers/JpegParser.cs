using System.Buffers.Binary;
using Lab2.Core.Contracts;
using Lab2.Core.Models;

namespace Lab2.Infrastructure.Parsers;

/// <summary>Reads JPEG markers up to SOS; checks the EOI marker at the file end.</summary>
public sealed class JpegParser : IImageParser
{
    public ImageFormat Format => ImageFormat.Jpeg;

    public bool CanParse(ReadOnlySpan<byte> signature) =>
        signature.Length >= 3 && signature[0] == 0xff && signature[1] == 0xd8 &&
        signature[2] == 0xff;

    public async ValueTask<ImageMetadata> ParseAsync(Stream stream, string filePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        if (!stream.CanRead || !stream.CanSeek)
            throw new ArgumentException("JPEG requires a readable, seekable stream.", nameof(stream));

        var result = new ImageMetadata
        {
            FilePath = filePath, FileSizeBytes = stream.Length, Format = Format,
            Compression = "JPEG (DCT)"
        };
        ImageMetadata Corrupt(string reason) => result with
        {
            Status = FileProcessingStatus.Corrupted, ErrorMessage = reason
        };

        stream.Position = 0;
        var marker = new byte[2];
        if (!await ParserBytes.ReadAsync(stream, marker, cancellationToken) ||
            marker[0] != 0xff || marker[1] != 0xd8)
            return Corrupt("Нет маркера SOI.");

        var sawFrame = false;
        var sawScan = false;
        while (stream.Position < stream.Length)
        {
            if (!await ParserBytes.ReadAsync(stream, marker.AsMemory(0, 1), cancellationToken) ||
                marker[0] != 0xff)
                return Corrupt("Ожидался маркер JPEG.");
            do
            {
                if (!await ParserBytes.ReadAsync(stream, marker.AsMemory(1, 1), cancellationToken))
                    return Corrupt("Обрезан маркер JPEG.");
            } while (marker[1] == 0xff);

            var code = marker[1];
            if (code is 0x00 or 0xd8 or 0xd9 or 0x01 || code is >= 0xd0 and <= 0xd7)
                return Corrupt("Неожиданный маркер перед данными JPEG.");

            if (!await ParserBytes.ReadAsync(stream, marker, cancellationToken))
                return Corrupt("Обрезана длина сегмента JPEG.");
            var segmentSize = BinaryPrimitives.ReadUInt16BigEndian(marker);
            if (segmentSize < 2 || segmentSize - 2 > stream.Length - stream.Position)
                return Corrupt("Длина сегмента JPEG выходит за пределы файла.");
            var payloadSize = segmentSize - 2;

            if (code == 0xe0 && payloadSize >= 14)
            {
                var app0 = new byte[14];
                if (!await ParserBytes.ReadAsync(stream, app0, cancellationToken))
                    return Corrupt("Обрезан сегмент APP0.");
                if (app0.AsSpan(0, 5).SequenceEqual("JFIF\0"u8))
                {
                    var units = app0[7];
                    var x = BinaryPrimitives.ReadUInt16BigEndian(app0.AsSpan(8, 2));
                    var y = BinaryPrimitives.ReadUInt16BigEndian(app0.AsSpan(10, 2));
                    if (units is 1 or 2 && x > 0 && y > 0)
                        result = result with
                        {
                            DpiX = units == 1 ? x : x * 2.54,
                            DpiY = units == 1 ? y : y * 2.54
                        };
                }
                if (!ParserBytes.Skip(stream, payloadSize - app0.Length))
                    return Corrupt("Обрезан сегмент APP0.");
                continue;
            }

            if (IsStartOfFrame(code))
            {
                if (sawFrame || payloadSize < 6)
                    return Corrupt("Повторный или короткий сегмент SOF.");
                var frame = new byte[6];
                if (!await ParserBytes.ReadAsync(stream, frame, cancellationToken))
                    return Corrupt("Обрезан сегмент SOF.");
                var precision = frame[0];
                var height = BinaryPrimitives.ReadUInt16BigEndian(frame.AsSpan(1, 2));
                var width = BinaryPrimitives.ReadUInt16BigEndian(frame.AsSpan(3, 2));
                var components = frame[5];
                if (width == 0 || height == 0 || precision == 0 || components == 0 ||
                    payloadSize != 6 + components * 3)
                    return Corrupt("Некорректные размеры или компоненты SOF.");
                result = result with
                {
                    Width = width, Height = height, ColorDepth = precision * components,
                    Compression = code switch
                    {
                        0xc0 => "JPEG (базовый DCT)",
                        0xc2 => "JPEG (прогрессивный DCT)",
                        0xc3 or 0xc7 or 0xcb or 0xcf => "JPEG (без потерь)",
                        _ => "JPEG (DCT)"
                    }
                };
                sawFrame = true;
                if (!ParserBytes.Skip(stream, payloadSize - frame.Length))
                    return Corrupt("Обрезан сегмент SOF.");
                continue;
            }

            if (code == 0xda)
            {
                if (!sawFrame || payloadSize < 6)
                    return Corrupt("Сегмент SOS без корректного SOF.");
                var scanHeader = new byte[1];
                if (!await ParserBytes.ReadAsync(stream, scanHeader, cancellationToken))
                    return Corrupt("Обрезан сегмент SOS.");
                if (scanHeader[0] == 0 || payloadSize != 1 + 2 * scanHeader[0] + 3 ||
                    !ParserBytes.Skip(stream, payloadSize - 1))
                    return Corrupt("Некорректная длина сегмента SOS.");
                sawScan = true;
                break;
            }

            if (!ParserBytes.Skip(stream, payloadSize))
                return Corrupt("Обрезан сегмент JPEG.");
        }

        if (!sawFrame || !sawScan || stream.Length - stream.Position < 3)
            return Corrupt("Отсутствуют SOF, SOS или данные JPEG.");

        stream.Seek(-2, SeekOrigin.End);
        if (!await ParserBytes.ReadAsync(stream, marker, cancellationToken) ||
            marker[0] != 0xff || marker[1] != 0xd9)
            return Corrupt("Отсутствует завершающий маркер EOI (FF D9).");

        return result;
    }

    private static bool IsStartOfFrame(byte code) =>
        code is >= 0xc0 and <= 0xcf and not (0xc4 or 0xc8 or 0xcc);
}
