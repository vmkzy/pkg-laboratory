namespace Lab2.Core.Models;

/// <summary>
/// Текущее состояние обхода папки для прогресс-бара и счетчиков интерфейса.
/// </summary>
public sealed record ScanProgress(
    int DiscoveredFiles,
    int ProcessedFiles,
    int ValidFiles,
    int ProblemFiles,
    bool EnumerationCompleted = true)
{
    public int Percentage => !EnumerationCompleted ? 0 : DiscoveredFiles == 0
        ? 100
        : Math.Clamp((int)((long)ProcessedFiles * 100 / DiscoveredFiles), 0, 100);
}
