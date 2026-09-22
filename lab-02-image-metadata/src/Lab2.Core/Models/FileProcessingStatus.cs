namespace Lab2.Core.Models;

/// <summary>
/// Результат проверки отдельного файла.
/// </summary>
public enum FileProcessingStatus
{
    Valid,
    Corrupted,
    Unsupported,
    Failed
}
