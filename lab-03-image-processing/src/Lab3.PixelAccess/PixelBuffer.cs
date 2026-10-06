namespace Lab3.PixelAccess;

public sealed class PixelBuffer
{
    public int Width { get; }
    public int Height { get; }

    // Порядок индексов: строка, столбец, канал (0 = R, 1 = G, 2 = B).
    public byte[,,] Pixels { get; }

    public PixelBuffer(int width, int height)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        Width = width;
        Height = height;
        Pixels = new byte[height, width, 3];
    }

    public RgbPixel this[int x, int y]
    {
        get => new(Pixels[y, x, 0], Pixels[y, x, 1], Pixels[y, x, 2]);
        set
        {
            Pixels[y, x, 0] = value.R;
            Pixels[y, x, 1] = value.G;
            Pixels[y, x, 2] = value.B;
        }
    }

    public RgbPixel Sample(int x, int y, EdgeMode mode)
    {
        x = EdgePadding.Map(x, Width, mode);
        y = EdgePadding.Map(y, Height, mode);
        return this[x, y];
    }
}
