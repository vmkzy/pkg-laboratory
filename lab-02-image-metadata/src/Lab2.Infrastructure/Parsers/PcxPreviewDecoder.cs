using System.Buffers.Binary;

namespace Lab2.Infrastructure.Parsers;

public sealed record PcxPreview(int Width, int Height, byte[] BgrPixels);

/// <summary>Decodes common 8- and 24-bit PCX files to a small preview buffer.</summary>
public static class PcxPreviewDecoder
{
    public static PcxPreview Decode(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (!stream.CanRead || !stream.CanSeek)
            throw new ArgumentException("PCX preview needs a readable, seekable stream.", nameof(stream));

        stream.Position = 0;
        var header = new byte[128];
        stream.ReadExactly(header);
        var bits = header[3];
        var planes = header[65];
        var indexed = bits == 8 && planes == 1 && header[1] == 5;
        var rgb = bits == 8 && planes == 3;
        if (header[0] != 0x0a || header[2] != 1 || !indexed && !rgb)
            throw new NotSupportedException("Предпросмотр доступен для 8- и 24-битных PCX с RLE.");

        int U16(int offset) => BinaryPrimitives.ReadUInt16LittleEndian(header.AsSpan(offset, 2));
        var width = U16(8) - U16(4) + 1;
        var height = U16(10) - U16(6) + 1;
        var bytesPerLine = U16(66);
        if (width < 1 || height < 1 || bytesPerLine < width ||
            (bytesPerLine & 1) != 0 || (long)width * height > 50_000_000)
            throw new InvalidDataException("Некорректные размеры PCX для предпросмотра.");

        byte[]? palette = null;
        var dataEnd = stream.Length;
        if (indexed)
        {
            if (dataEnd < 128 + 769)
                throw new InvalidDataException("Отсутствует палитра PCX.");
            stream.Seek(-769, SeekOrigin.End);
            if (stream.ReadByte() != 0x0c)
                throw new InvalidDataException("Отсутствует маркер палитры PCX.");
            palette = new byte[768];
            stream.ReadExactly(palette);
            dataEnd -= 769;
        }

        stream.Position = 128;
        var scale = Math.Min(1.0, Math.Min(320.0 / width, 240.0 / height));
        var previewWidth = Math.Max(1, (int)Math.Floor(width * scale));
        var previewHeight = Math.Max(1, (int)Math.Floor(height * scale));
        var pixels = new byte[previewWidth * previewHeight * 3];
        var line = new byte[bytesPerLine * planes];
        var repeatRemaining = 0;
        var repeatedValue = 0;
        var nextPreviewRow = 0;

        for (var y = 0; y < height; y++)
        {
            for (var index = 0; index < line.Length; index++)
            {
                if (repeatRemaining == 0)
                {
                    if (stream.Position >= dataEnd)
                        throw new InvalidDataException("Данные PCX обрезаны.");
                    var code = stream.ReadByte();
                    if ((code & 0xc0) == 0xc0)
                    {
                        repeatRemaining = code & 0x3f;
                        if (repeatRemaining == 0 || stream.Position >= dataEnd)
                            throw new InvalidDataException("Некорректная серия RLE в PCX.");
                        repeatedValue = stream.ReadByte();
                    }
                    else
                    {
                        repeatRemaining = 1;
                        repeatedValue = code;
                    }
                }
                line[index] = (byte)repeatedValue;
                repeatRemaining--;
            }

            if (nextPreviewRow >= previewHeight ||
                y != (long)nextPreviewRow * height / previewHeight)
                continue;

            for (var x = 0; x < previewWidth; x++)
            {
                var sourceX = (int)((long)x * width / previewWidth);
                var target = (nextPreviewRow * previewWidth + x) * 3;
                if (indexed)
                {
                    var color = line[sourceX] * 3;
                    pixels[target] = palette![color + 2];
                    pixels[target + 1] = palette[color + 1];
                    pixels[target + 2] = palette[color];
                }
                else
                {
                    pixels[target] = line[2 * bytesPerLine + sourceX];
                    pixels[target + 1] = line[bytesPerLine + sourceX];
                    pixels[target + 2] = line[sourceX];
                }
            }
            nextPreviewRow++;
        }

        return new PcxPreview(previewWidth, previewHeight, pixels);
    }
}
