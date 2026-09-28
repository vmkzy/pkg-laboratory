using System.Buffers.Binary;
using Lab2.Core.Contracts;
using Lab2.Core.Models;

namespace Lab2.Infrastructure.Parsers;

/// <summary>Reads the fixed PCX header and checks the optional 256-color palette.</summary>
public sealed class PcxParser : IImageParser
{
    public ImageFormat Format => ImageFormat.Pcx;

    public bool CanParse(ReadOnlySpan<byte> signature) =>
        signature.Length >= 4 && signature[0] == 0x0a &&
        signature[1] is 0 or 2 or 3 or 4 or 5 &&
        signature[3] is 1 or 2 or 4 or 8;

    public async ValueTask<ImageMetadata> ParseAsync(Stream stream, string filePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        if (!stream.CanRead || !stream.CanSeek)
            throw new ArgumentException("PCX requires a readable, seekable stream.", nameof(stream));

        var result = new ImageMetadata
        {
            FilePath = filePath, FileSizeBytes = stream.Length,
            Format = Format, Compression = "RLE (PCX)"
        };
        ImageMetadata Corrupt(string reason) => result with
        {
            Status = FileProcessingStatus.Corrupted, ErrorMessage = reason
        };

        stream.Position = 0;
        var header = new byte[128];
        if (!await ParserBytes.ReadAsync(stream, header, cancellationToken))
            return Corrupt("PCX короче 128-байтного заголовка.");
        if (!CanParse(header))
            return Corrupt("Нет корректной сигнатуры или глубины PCX.");

        var xmin = U16(header, 4);
        var ymin = U16(header, 6);
        var xmax = U16(header, 8);
        var ymax = U16(header, 10);
        var width = (int)xmax - xmin + 1;
        var height = (int)ymax - ymin + 1;
        var bitsPerPlane = header[3];
        var planes = header[65];
        var bytesPerLine = U16(header, 66);
        var horizontalDpi = U16(header, 12);
        var verticalDpi = U16(header, 14);

        result = result with
        {
            Width = width, Height = height,
            ColorDepth = bitsPerPlane * planes,
            DpiX = horizontalDpi == 0 ? null : horizontalDpi,
            DpiY = verticalDpi == 0 ? null : verticalDpi
        };

        if (width <= 0 || height <= 0 || planes is < 1 or > 4 ||
            bytesPerLine == 0 || (bytesPerLine & 1) != 0 ||
            bytesPerLine < ((long)width * bitsPerPlane + 7) / 8)
            return Corrupt("Некорректные размеры, плоскости или длина строки PCX.");
        if (header[2] != 1)
            return result with
            {
                Status = FileProcessingStatus.Unsupported,
                ErrorMessage = $"Способ кодирования PCX {header[2]} не поддерживается."
            };

        var imageDataEnd = stream.Length;
        if (header[1] == 5 && bitsPerPlane == 8 && planes == 1)
        {
            if (stream.Length < 128 + 769 + 1)
                return Corrupt("Отсутствует палитра 256-цветного PCX.");
            stream.Seek(-769, SeekOrigin.End);
            var marker = new byte[1];
            if (!await ParserBytes.ReadAsync(stream, marker, cancellationToken) || marker[0] != 0x0c)
                return Corrupt("Отсутствует маркер палитры 256-цветного PCX.");
            imageDataEnd -= 769;
        }

        // A run stores at most 63 decoded bytes in two encoded bytes. This detects
        // clearly truncated data without decoding the complete pixel stream.
        var decodedBytes = (long)bytesPerLine * planes * height;
        var minimumEncoded = Math.Min(decodedBytes, 2 * ((decodedBytes + 62) / 63));
        if (imageDataEnd - 128 < minimumEncoded)
            return Corrupt("Данные изображения PCX слишком короткие.");

        return result;
    }

    private static ushort U16(byte[] bytes, int offset) =>
        BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(offset, 2));
}
