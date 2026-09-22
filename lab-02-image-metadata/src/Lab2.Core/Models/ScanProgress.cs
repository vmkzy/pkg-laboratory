namespace Lab2.Core.Models;

/// <summary>
/// Текущее состояние обхода папки для прогресс-бара и счетчиков интерфейса.
/// </summary>
public sealed record ScanProgress(
    int DiscoveredFiles,
    int ProcessedFiles,
    int ValidFiles,
    int ProblemFiles)
{
    public int Percentage => DiscoveredFiles == 0
        ? 0
        : Math.Clamp(ProcessedFiles * 100 / DiscoveredFiles, 0, 100);
}
