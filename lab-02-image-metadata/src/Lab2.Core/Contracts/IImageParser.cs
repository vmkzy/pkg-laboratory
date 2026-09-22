using Lab2.Core.Models;

namespace Lab2.Core.Contracts;

/// <summary>
/// Контракт ручного побайтового парсера одного графического формата.
/// </summary>
public interface IImageParser
{
    ImageFormat Format { get; }

    bool CanParse(ReadOnlySpan<byte> signature);

    ValueTask<ImageMetadata> ParseAsync(
        Stream stream,
        string filePath,
        CancellationToken cancellationToken = default);
}
