using Lab3.PixelAccess;

namespace Lab3.Processing;

public static class AdaptiveThresholding
{
    // Саувола: T = m * (1 + k * (s / R - 1)), R = 128 для яркости 0..255.
    public static GrayBuffer Apply(GrayBuffer source, int radius, double k, EdgeMode edgeMode,
        bool parallel = true, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (radius < 1 || radius > 100)
            throw new ArgumentOutOfRangeException(nameof(radius));
        if (!double.IsFinite(k) || k < 0 || k > 1)
            throw new ArgumentOutOfRangeException(nameof(k));
        var integral = new IntegralImage(source, radius, edgeMode, token);
        var result = new GrayBuffer(source.Width, source.Height);
        RowProcessing.For(source.Width, source.Height, parallel, token, y =>
        {
            for (var x = 0; x < source.Width; x++)
            {
                var (mean, deviation) = integral.GetWindowStatistics(x, y, radius);
                var threshold = mean * (1 + k * (deviation / 128 - 1));
                result.Pixels[y, x] = source.Pixels[y, x] > threshold ? (byte)255 : (byte)0;
            }
        });
        return result;
    }
}
