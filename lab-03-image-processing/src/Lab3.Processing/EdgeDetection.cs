using Lab3.PixelAccess;

namespace Lab3.Processing;

public sealed record GradientField(DoubleBuffer X, DoubleBuffer Y, DoubleBuffer Magnitude, DoubleBuffer Angle);
public sealed record EdgePoint(double X, double Y, double Angle, double Strength);
public sealed record EdgeResult(GrayBuffer Image, PixelBuffer Directions, IReadOnlyList<EdgePoint> Points);

public static class EdgeDetection
{
    private static readonly int[,] SobelX = { { -1, 0, 1 }, { -2, 0, 2 }, { -1, 0, 1 } };
    private static readonly int[,] SobelY = { { -1, -2, -1 }, { 0, 0, 0 }, { 1, 2, 1 } };

    public static GradientField Sobel(DoubleBuffer source, EdgeMode mode, bool parallel = true,
        CancellationToken token = default)
    {
        if (!Enum.IsDefined(mode))
            throw new ArgumentOutOfRangeException(nameof(mode));
        var gx = new DoubleBuffer(source.Width, source.Height);
        var gy = new DoubleBuffer(source.Width, source.Height);
        var magnitude = new DoubleBuffer(source.Width, source.Height);
        var angle = new DoubleBuffer(source.Width, source.Height);
        RowProcessing.For(source.Width, source.Height, parallel, token, y =>
        {
            for (var x = 0; x < source.Width; x++)
            {
                double sx = 0, sy = 0;
                for (var dy = -1; dy <= 1; dy++)
                    for (var dx = -1; dx <= 1; dx++)
                    {
                        var value = source.Sample(x + dx, y + dy, mode);
                        sx += SobelX[dy + 1, dx + 1] * value;
                        sy += SobelY[dy + 1, dx + 1] * value;
                    }
                gx.Pixels[y, x] = sx;
                gy.Pixels[y, x] = sy;
                magnitude.Pixels[y, x] = Math.Sqrt(sx * sx + sy * sy);
                angle.Pixels[y, x] = Math.Atan2(sy, sx);
            }
        });
        return new(gx, gy, magnitude, angle);
    }

    public static EdgeResult Detect(GrayBuffer source, bool canny, double sigma, double low, double high,
        EdgeMode mode, bool parallel = true, CancellationToken token = default)
    {
        if (!double.IsFinite(high) || high <= 0 || !double.IsFinite(low) || low <= 0 || low > high)
            throw new ArgumentOutOfRangeException(nameof(high));
        DoubleBuffer input;
        if (canny)
            input = SpatialFilters.Gaussian(source, sigma, mode, parallel, token);
        else
        {
            input = new DoubleBuffer(source.Width, source.Height);
            RowProcessing.For(source.Width, source.Height, parallel, token, y =>
            {
                for (var x = 0; x < source.Width; x++)
                    input.Pixels[y, x] = source.Pixels[y, x];
            });
        }
        var gradient = Sobel(input, mode, parallel, token);
        var response = canny ? SuppressNonMaxima(gradient, mode, parallel, token) : gradient.Magnitude;
        var edges = canny ? Hysteresis(response, low, high, token) : new GrayBuffer(source.Width, source.Height);
        if (!canny)
            RowProcessing.For(source.Width, source.Height, parallel, token, y =>
            {
                for (var x = 0; x < source.Width; x++)
                    edges.Pixels[y, x] = response.Pixels[y, x] >= high ? (byte)255 : (byte)0;
            });
        var directions = new PixelBuffer(source.Width, source.Height);
        RowProcessing.For(source.Width, source.Height, parallel, token, y =>
        {
            for (var x = 0; x < source.Width; x++)
                if (edges.Pixels[y, x] != 0)
                    directions[x, y] = DirectionColor(gradient.Angle.Pixels[y, x]);
        });

        var points = new List<EdgePoint>();
        if (canny)
            for (var y = 0; y < source.Height; y++)
            {
                token.ThrowIfCancellationRequested();
                for (var x = 0; x < source.Width; x++)
                    if (edges.Pixels[y, x] != 0)
                    {
                        var strength = gradient.Magnitude.Pixels[y, x];
                        var nx = gradient.X.Pixels[y, x] / strength;
                        var ny = gradient.Y.Pixels[y, x] / strength;
                        var minus = gradient.Magnitude.SampleBilinear(x - nx, y - ny, mode);
                        var plus = gradient.Magnitude.SampleBilinear(x + nx, y + ny, mode);
                        // Вершина параболы через три отсчёта вдоль нормали к границе.
                        var denominator = 2 * (minus - 2 * strength + plus);
                        var offset = Math.Abs(denominator) < 1e-12 ? 0 :
                            Math.Clamp((minus - plus) / denominator, -0.5, 0.5);
                        points.Add(new(x + offset * nx, y + offset * ny, gradient.Angle.Pixels[y, x], strength));
                    }
            }
        return new(edges, directions, points);
    }

    public static DoubleBuffer SuppressNonMaxima(GradientField gradient, EdgeMode mode,
        bool parallel = true, CancellationToken token = default)
    {
        var source = gradient.Magnitude;
        var result = new DoubleBuffer(source.Width, source.Height);
        RowProcessing.For(source.Width, source.Height, parallel, token, y =>
        {
            for (var x = 0; x < source.Width; x++)
            {
                var center = source.Pixels[y, x];
                if (center <= 0) continue;
                var nx = gradient.X.Pixels[y, x] / center;
                var ny = gradient.Y.Pixels[y, x] / center;
                var minus = source.SampleBilinear(x - nx, y - ny, mode);
                var plus = source.SampleBilinear(x + nx, y + ny, mode);
                if (center >= minus && center > plus)
                    result.Pixels[y, x] = center;
            }
        });
        return result;
    }

    public static GrayBuffer Hysteresis(DoubleBuffer response, double low, double high,
        CancellationToken token = default)
    {
        if (!double.IsFinite(low) || !double.IsFinite(high) || low <= 0 || high < low)
            throw new ArgumentOutOfRangeException(nameof(low));
        var result = new GrayBuffer(response.Width, response.Height);
        var queue = new Queue<(int X, int Y)>();
        for (var y = 0; y < response.Height; y++)
        {
            token.ThrowIfCancellationRequested();
            for (var x = 0; x < response.Width; x++)
                if (response.Pixels[y, x] >= high)
                {
                    result.Pixels[y, x] = 255;
                    queue.Enqueue((x, y));
                }
        }
        while (queue.TryDequeue(out var point))
        {
            token.ThrowIfCancellationRequested();
            for (var dy = -1; dy <= 1; dy++)
                for (var dx = -1; dx <= 1; dx++)
                {
                    var x = point.X + dx;
                    var y = point.Y + dy;
                    if (x < 0 || y < 0 || x >= response.Width || y >= response.Height) continue;
                    if (result.Pixels[y, x] == 0 && response.Pixels[y, x] >= low)
                    {
                        result.Pixels[y, x] = 255;
                        queue.Enqueue((x, y));
                    }
                }
        }
        return result;
    }

    public static RgbPixel DirectionColor(double angle)
    {
        var hue = ((angle / (2 * Math.PI)) % 1 + 1) % 1 * 6;
        var sector = (int)hue;
        var ascending = (byte)Math.Round(255 * (hue - sector));
        var descending = (byte)(255 - ascending);
        return sector switch
        {
            0 => new(255, ascending, 0), 1 => new(descending, 255, 0),
            2 => new(0, 255, ascending), 3 => new(0, descending, 255),
            4 => new(ascending, 0, 255), _ => new(255, 0, descending)
        };
    }
}
