namespace Lab3.PixelAccess;

// Промежуточные результаты фильтров не округляются до байта.
public sealed class DoubleBuffer
{
    public int Width { get; }
    public int Height { get; }
    public double[,] Pixels { get; }

    public DoubleBuffer(int width, int height)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        Width = width;
        Height = height;
        Pixels = new double[height, width];
    }

    public double Sample(int x, int y, EdgeMode mode) =>
        Pixels[EdgePadding.Map(y, Height, mode), EdgePadding.Map(x, Width, mode)];

    public double SampleBilinear(double x, double y, EdgeMode mode)
    {
        var left = (int)Math.Floor(x);
        var top = (int)Math.Floor(y);
        var dx = x - left;
        var dy = y - top;
        return (1 - dy) * ((1 - dx) * Sample(left, top, mode) + dx * Sample(left + 1, top, mode))
            + dy * ((1 - dx) * Sample(left, top + 1, mode) + dx * Sample(left + 1, top + 1, mode));
    }
}
