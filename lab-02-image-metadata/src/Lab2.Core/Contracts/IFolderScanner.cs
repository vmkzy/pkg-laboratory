using Lab2.Core.Models;

namespace Lab2.Core.Contracts;

/// <summary>
/// Оркестратор обхода папки и запуска парсеров.
/// </summary>
public interface IFolderScanner
{
    IAsyncEnumerable<ImageMetadata> ScanAsync(
        ScanRequest request,
        IProgress<ScanProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
