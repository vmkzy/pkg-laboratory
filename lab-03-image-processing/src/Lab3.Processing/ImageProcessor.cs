using Lab3.PixelAccess;

namespace Lab3.Processing;

public enum ProcessingMethod { Otsu, Iterative, Sauvola, Points, Lines, Sobel, Canny }

public sealed record ProcessingSettings
{
    public ProcessingMethod Method { get; init; }
    public int Radius { get; init; } = 7;
    public double K { get; init; } = 0.2;
    public double Sigma { get; init; } = 1.2;
    public double LowThreshold { get; init; } = 30;
    public double HighThreshold { get; init; } = 80;
    public EdgeMode EdgeMode { get; init; } = EdgeMode.Reflect;
    public LineDirection LineDirection { get; init; }
    public bool Parallel { get; init; } = true;
}

public sealed record ProcessingResult(GrayBuffer Image, long[] BeforeHistogram, long[] AfterHistogram,
    int? Threshold, PixelBuffer? Directions, IReadOnlyList<EdgePoint> EdgePoints);

public static class ImageProcessor
{
    public static ProcessingResult Process(PixelBuffer source, ProcessingSettings settings,
        CancellationToken token = default, IProgress<int>? progress = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(settings);
        if (!Enum.IsDefined(settings.Method) || !Enum.IsDefined(settings.EdgeMode))
            throw new ArgumentOutOfRangeException(nameof(settings));
        progress?.Report(5);
        var gray = GrayscaleConverter.Convert(source, token, settings.Parallel);
        var before = ImageHistogram.Build(gray, token);
        progress?.Report(20);
        GrayBuffer image;
        PixelBuffer? directions = null;
        IReadOnlyList<EdgePoint> points = Array.Empty<EdgePoint>();
        int? threshold = null;
        switch (settings.Method)
        {
            case ProcessingMethod.Otsu:
            case ProcessingMethod.Iterative:
                threshold = settings.Method == ProcessingMethod.Otsu
                    ? GlobalThresholding.FindOtsuThreshold(before) : GlobalThresholding.FindIterativeThreshold(before);
                image = new GrayBuffer(gray.Width, gray.Height);
                RowProcessing.For(gray.Width, gray.Height, settings.Parallel, token, y =>
                {
                    for (var x = 0; x < gray.Width; x++)
                        image.Pixels[y, x] = gray.Pixels[y, x] > threshold.Value ? (byte)255 : (byte)0;
                });
                break;
            case ProcessingMethod.Sauvola:
                image = AdaptiveThresholding.Apply(gray, settings.Radius, settings.K, settings.EdgeMode, settings.Parallel, token);
                break;
            case ProcessingMethod.Points:
            case ProcessingMethod.Lines:
                image = SpatialFilters.Detect(gray, settings.Method == ProcessingMethod.Points, settings.LineDirection,
                    settings.HighThreshold, settings.EdgeMode, settings.Parallel, token);
                break;
            default:
                var edges = EdgeDetection.Detect(gray, settings.Method == ProcessingMethod.Canny,
                    settings.Sigma, settings.LowThreshold, settings.HighThreshold,
                    settings.EdgeMode, settings.Parallel, token);
                image = edges.Image;
                directions = edges.Directions;
                points = edges.Points;
                break;
        }
        progress?.Report(85);
        var after = ImageHistogram.Build(image, token);
        progress?.Report(100);
        return new(image, before, after, threshold, directions, points);
    }
}
