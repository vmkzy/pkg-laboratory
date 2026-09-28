using System.Buffers.Binary;
using Lab2.Core.Contracts;
using Lab2.Core.Models;

namespace Lab2.Infrastructure.Parsers;

/// <summary>Reads the first classic TIFF IFD and verifies referenced image strips or tiles.</summary>
public sealed class TiffParser : IImageParser
{
    private static readonly HashSet<ushort> NeededTags =
    [
        256, 257, 258, 259, 273, 277, 279, 282, 283, 296, 324, 325
    ];

    private sealed record Field(ushort Type, uint Count, byte[] Value);

    public ImageFormat Format => ImageFormat.Tiff;

    public bool CanParse(ReadOnlySpan<byte> signature) => signature.Length >= 4 &&
        (signature[0] == 0x49 && signature[1] == 0x49 &&
         signature[2] is 0x2a or 0x2b && signature[3] == 0 ||
         signature[0] == 0x4d && signature[1] == 0x4d &&
         signature[2] == 0 && signature[3] is 0x2a or 0x2b);

    public async ValueTask<ImageMetadata> ParseAsync(Stream stream, string filePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        if (!stream.CanRead || !stream.CanSeek)
            throw new ArgumentException("TIFF requires a readable, seekable stream.", nameof(stream));

        var result = new ImageMetadata
        {
            FilePath = filePath, FileSizeBytes = stream.Length, Format = Format
        };
        ImageMetadata Corrupt(string reason) => result with
        {
            Status = FileProcessingStatus.Corrupted, ErrorMessage = reason
        };

        stream.Position = 0;
        var header = new byte[8];
        if (!await ParserBytes.ReadAsync(stream, header, cancellationToken) || !CanParse(header))
            return Corrupt("Нет полного заголовка TIFF.");
        var littleEndian = header[0] == 0x49;
        if (U16(header, 2, littleEndian) == 43)
            return result with
            {
                Status = FileProcessingStatus.Unsupported,
                ErrorMessage = "BigTIFF требует 64-битных смещений и пока не поддерживается."
            };

        var firstIfd = U32(header, 4, littleEndian);
        if (firstIfd < 8 || (firstIfd & 1) != 0 || firstIfd > stream.Length - 6)
            return Corrupt("Указатель на первый IFD выходит за пределы TIFF.");

        stream.Position = firstIfd;
        var countBytes = new byte[2];
        if (!await ParserBytes.ReadAsync(stream, countBytes, cancellationToken))
            return Corrupt("Обрезано количество тегов IFD.");
        var count = U16(countBytes, 0, littleEndian);
        if (count == 0 || (long)firstIfd + 2 + count * 12L + 4 > stream.Length)
            return Corrupt("Таблица тегов IFD обрезана или пуста.");

        var fields = new Dictionary<ushort, Field>();
        var entry = new byte[12];
        for (var index = 0; index < count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!await ParserBytes.ReadAsync(stream, entry, cancellationToken))
                return Corrupt("Обрезана запись IFD.");
            var tag = U16(entry, 0, littleEndian);
            var type = U16(entry, 2, littleEndian);
            var valueCount = U32(entry, 4, littleEndian);
            var value = entry.AsSpan(8, 4).ToArray();
            var elementSize = TypeSize(type);
            if (elementSize != 0 && valueCount > 0 && (long)elementSize * valueCount > 4)
            {
                var offset = U32(value, 0, littleEndian);
                if (offset < 8 || (long)offset + elementSize * (long)valueCount > stream.Length)
                    return Corrupt($"Данные тега {tag} выходят за пределы TIFF.");
            }
            if (!NeededTags.Contains(tag)) continue;
            if (!fields.TryAdd(tag, new Field(type, valueCount, value)))
                return Corrupt($"Тег {tag} повторяется в IFD.");
        }

        var next = new byte[4];
        if (!await ParserBytes.ReadAsync(stream, next, cancellationToken))
            return Corrupt("Обрезан указатель на следующий IFD.");
        var nextIfd = U32(next, 0, littleEndian);
        if (nextIfd != 0 && (nextIfd < 8 || (nextIfd & 1) != 0 ||
                             nextIfd > stream.Length - 6))
            return Corrupt("Указатель на следующую страницу TIFF некорректен.");

        var width = await Single(fields, 256, stream, littleEndian, cancellationToken);
        var height = await Single(fields, 257, stream, littleEndian, cancellationToken);
        var samples = fields.ContainsKey(277)
            ? await Single(fields, 277, stream, littleEndian, cancellationToken) : 1u;
        if (width is null or 0 || height is null or 0 ||
            width > int.MaxValue || height > int.MaxValue || samples is null or 0 or > 16)
            return Corrupt("Отсутствуют или некорректны размеры и компоненты TIFF.");

        var depth = 0;
        if (fields.TryGetValue(258, out var bitsField))
        {
            if (bitsField.Type != 3 || bitsField.Count != samples)
                return Corrupt("BitsPerSample не соответствует SamplesPerPixel.");
            for (uint index = 0; index < samples; index++)
            {
                var bits = await UnsignedAt(bitsField, index, stream, littleEndian, cancellationToken);
                if (bits is null or 0 or > 64)
                    return Corrupt("Некорректная глубина компоненты TIFF.");
                depth += (int)bits.Value;
            }
        }
        else
        {
            depth = (int)samples.Value; // TIFF default: one bit per sample.
        }

        var compression = fields.ContainsKey(259)
            ? await Single(fields, 259, stream, littleEndian, cancellationToken) : 1u;
        var unit = fields.ContainsKey(296)
            ? await Single(fields, 296, stream, littleEndian, cancellationToken) : 2u;
        if (compression is null or 0 || unit is null or < 1 or > 3)
            return Corrupt("Некорректные теги сжатия или единиц разрешения TIFF.");

        var xResolution = fields.TryGetValue(282, out var xField)
            ? await Rational(xField, stream, littleEndian, cancellationToken) : null;
        var yResolution = fields.TryGetValue(283, out var yField)
            ? await Rational(yField, stream, littleEndian, cancellationToken) : null;
        if (xField is not null && xResolution is null ||
            yField is not null && yResolution is null)
            return Corrupt("Некорректная дробь XResolution или YResolution.");

        result = result with
        {
            Width = (int)width.Value, Height = (int)height.Value,
            ColorDepth = depth, Compression = CompressionName(compression.Value),
            DpiX = unit == 1 ? null : xResolution * (unit == 3 ? 2.54 : 1),
            DpiY = unit == 1 ? null : yResolution * (unit == 3 ? 2.54 : 1)
        };

        var stripOffsets = fields.GetValueOrDefault((ushort)273);
        var stripSizes = fields.GetValueOrDefault((ushort)279);
        var tileOffsets = fields.GetValueOrDefault((ushort)324);
        var tileSizes = fields.GetValueOrDefault((ushort)325);
        var hasStrips = stripOffsets is not null || stripSizes is not null;
        var hasTiles = tileOffsets is not null || tileSizes is not null;
        var offsets = hasStrips ? stripOffsets : tileOffsets;
        var sizes = hasStrips ? stripSizes : tileSizes;
        if (offsets is null || sizes is null || offsets.Count == 0 ||
            offsets.Count != sizes.Count || offsets.Type is not (3 or 4) ||
            sizes.Type is not (3 or 4) || hasStrips && hasTiles)
            return Corrupt("Отсутствуют или некорректны смещения полос/плиток TIFF.");

        for (uint index = 0; index < offsets.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var offset = await UnsignedAt(offsets, index, stream, littleEndian, cancellationToken);
            var size = await UnsignedAt(sizes, index, stream, littleEndian, cancellationToken);
            if (offset is null or < 8 || size is null or 0 ||
                (long)offset + size > stream.Length)
                return Corrupt("Полоса или плитка TIFF выходит за пределы файла.");
        }

        return IsKnownCompression(compression.Value) ? result : result with
        {
            Status = FileProcessingStatus.Unsupported,
            ErrorMessage = $"Код сжатия TIFF {compression} пока не поддерживается."
        };
    }

    private static async ValueTask<uint?> Single(Dictionary<ushort, Field> fields, ushort tag,
        Stream stream, bool littleEndian, CancellationToken cancellationToken)
    {
        if (!fields.TryGetValue(tag, out var field) || field.Count != 1)
            return null;
        return await UnsignedAt(field, 0, stream, littleEndian, cancellationToken);
    }

    private static async ValueTask<uint?> UnsignedAt(Field field, uint index, Stream stream,
        bool littleEndian, CancellationToken cancellationToken)
    {
        var size = field.Type switch { 3 => 2, 4 => 4, _ => 0 };
        if (size == 0 || index >= field.Count) return null;
        if ((long)size * field.Count <= 4)
            return size == 2 ? U16(field.Value, (int)index * 2, littleEndian)
                             : U32(field.Value, 0, littleEndian);

        var offset = U32(field.Value, 0, littleEndian);
        stream.Position = (long)offset + index * size;
        var buffer = new byte[size];
        if (!await ParserBytes.ReadAsync(stream, buffer, cancellationToken)) return null;
        return size == 2 ? U16(buffer, 0, littleEndian) : U32(buffer, 0, littleEndian);
    }

    private static async ValueTask<double?> Rational(Field field, Stream stream,
        bool littleEndian, CancellationToken cancellationToken)
    {
        if (field.Type != 5 || field.Count != 1) return null;
        stream.Position = U32(field.Value, 0, littleEndian);
        var buffer = new byte[8];
        if (!await ParserBytes.ReadAsync(stream, buffer, cancellationToken)) return null;
        var numerator = U32(buffer, 0, littleEndian);
        var denominator = U32(buffer, 4, littleEndian);
        return denominator == 0 ? null : (double)numerator / denominator;
    }

    private static int TypeSize(ushort type) => type switch
    {
        1 or 2 or 6 or 7 => 1,
        3 or 8 => 2,
        4 or 9 or 11 => 4,
        5 or 10 or 12 => 8,
        _ => 0
    };

    private static ushort U16(byte[] bytes, int offset, bool littleEndian) => littleEndian
        ? BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(offset, 2))
        : BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(offset, 2));

    private static uint U32(byte[] bytes, int offset, bool littleEndian) => littleEndian
        ? BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(offset, 4))
        : BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(offset, 4));

    private static bool IsKnownCompression(uint code) =>
        code is 1 or 2 or 3 or 4 or 5 or 6 or 7 or 8 or 32773 or 32946;

    private static string CompressionName(uint code) => code switch
    {
        1 => "Без сжатия", 2 => "CCITT 1D", 3 => "CCITT Group 3",
        4 => "CCITT Group 4", 5 => "LZW", 6 => "JPEG (старый TIFF)",
        7 => "JPEG", 8 or 32946 => "Deflate", 32773 => "PackBits",
        _ => $"Неизвестный код ({code})"
    };
}
