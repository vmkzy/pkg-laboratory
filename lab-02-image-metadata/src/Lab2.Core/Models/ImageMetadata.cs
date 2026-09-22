namespace Lab2.Core.Models;

/// <summary>
/// Метаданные, прочитанные непосредственно из байтов графического файла.
/// </summary>
public sealed record ImageMetadata
{
    public required string FilePath { get; init; }

    public string FileName => Path.GetFileName(FilePath);

    public ImageFormat Format { get; init; } = ImageFormat.Unknown;

    public long FileSizeBytes { get; init; }

    public int? Width { get; init; }

    public int? Height { get; init; }

    public double? DpiX { get; init; }

    public double? DpiY { get; init; }

    public int? ColorDepth { get; init; }

    public string Compression { get; init; } = "Не определено";

    public FileProcessingStatus Status { get; init; } = FileProcessingStatus.Valid;

    public string? ErrorMessage { get; init; }
}
