using Lab3.PixelAccess;

namespace Lab3.Processing;

public enum LineDirection { All, Horizontal, Vertical, DiagonalDown, DiagonalUp }

public static class SpatialFilters
{
    private static readonly int[][,] LineMasks =
    [
        new int[,] { { -1, -1, -1 }, { 2, 2, 2 }, { -1, -1, -1 } },
        new int[,] { { -1, 2, -1 }, { -1, 2, -1 }, { -1, 2, -1 } },
        new int[,] { { 2, -1, -1 }, { -1, 2, -1 }, { -1, -1, 2 } },
        new int[,] { { -1, -1, 2 }, { -1, 2, -1 }, { 2, -1, -1 } }
    ];

    public static GrayBuffer Detect(GrayBuffer source, bool points, LineDirection direction,
        double threshold, EdgeMode mode, bool parallel = true, CancellationToken token = default)
    {
        if (!Enum.IsDefined(direction) || !Enum.IsDefined(mode) || !double.IsFinite(threshold) || threshold <= 0)
            throw new ArgumentOutOfRangeException(nameof(threshold));
        var result = new GrayBuffer(source.Width, source.Height);
        RowProcessing.For(source.Width, source.Height, parallel, token, y =>
        {
            for (var x = 0; x < source.Width; x++)
            {
                double response = 0;
                if (points)
                {
                    response = 8 * source.Pixels[y, x];
                    for (var dy = -1; dy <= 1; dy++)
                        for (var dx = -1; dx <= 1; dx++)
                            if (dx != 0 || dy != 0)
                                response -= source.Sample(x + dx, y + dy, mode);
                    response = Math.Abs(response);
                }
                else
                {
                    var first = direction == LineDirection.All ? 0 : (int)direction - 1;
                    var last = direction == LineDirection.All ? 3 : first;
                    for (var mask = first; mask <= last; mask++)
                    {
                        double sum = 0;
                        for (var dy = -1; dy <= 1; dy++)
                            for (var dx = -1; dx <= 1; dx++)
                                sum += LineMasks[mask][dy + 1, dx + 1] * source.Sample(x + dx, y + dy, mode);
                        response = Math.Max(response, Math.Abs(sum));
                    }
                }
                result.Pixels[y, x] = response >= threshold ? (byte)255 : (byte)0;
            }
        });
        return result;
    }

    public static double[] GaussianKernel(double sigma)
    {
        if (!double.IsFinite(sigma) || sigma < 0.3 || sigma > 10)
            throw new ArgumentOutOfRangeException(nameof(sigma));
        var radius = (int)Math.Ceiling(3 * sigma);
        var kernel = new double[2 * radius + 1];
        double total = 0;
        for (var i = -radius; i <= radius; i++)
        {
            kernel[i + radius] = Math.Exp(-i * i / (2 * sigma * sigma));
            total += kernel[i + radius];
        }
        for (var i = 0; i < kernel.Length; i++)
            kernel[i] /= total;
        return kernel;
    }

    public static DoubleBuffer Gaussian(GrayBuffer source, double sigma, EdgeMode mode,
        bool parallel = true, CancellationToken token = default)
    {
        if (!Enum.IsDefined(mode))
            throw new ArgumentOutOfRangeException(nameof(mode));
        var kernel = GaussianKernel(sigma);
        var radius = kernel.Length / 2;
        var horizontal = new DoubleBuffer(source.Width, source.Height);
        var result = new DoubleBuffer(source.Width, source.Height);
        RowProcessing.For(source.Width, source.Height, parallel, token, y =>
        {
            for (var x = 0; x < source.Width; x++)
                for (var dx = -radius; dx <= radius; dx++)
                    horizontal.Pixels[y, x] += kernel[dx + radius] * source.Sample(x + dx, y, mode);
        });
        RowProcessing.For(source.Width, source.Height, parallel, token, y =>
        {
            for (var x = 0; x < source.Width; x++)
                for (var dy = -radius; dy <= radius; dy++)
                    result.Pixels[y, x] += kernel[dy + radius] * horizontal.Sample(x, y + dy, mode);
        });
        return result;
    }
}
