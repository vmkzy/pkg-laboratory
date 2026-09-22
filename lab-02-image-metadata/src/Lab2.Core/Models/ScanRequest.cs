namespace Lab2.Core.Models;

/// <summary>
/// Параметры одного запуска сканирования папки.
/// </summary>
public sealed record ScanRequest
{
    public required string FolderPath { get; init; }

    public bool IncludeSubdirectories { get; init; } = true;

    public int WorkerCount { get; init; } = Math.Max(1, Environment.ProcessorCount);
}
