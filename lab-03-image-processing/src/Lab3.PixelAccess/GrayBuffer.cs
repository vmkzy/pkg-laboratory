namespace Lab3.PixelAccess;

public sealed class GrayBuffer
{
    public int Width { get; }
    public int Height { get; }
    public byte[,] Pixels { get; }

    public GrayBuffer(int width, int height)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        Width = width;
        Height = height;
        Pixels = new byte[height, width];
    }

    public byte this[int x, int y]
    {
        get => Pixels[y, x];
        set => Pixels[y, x] = value;
    }

    public byte Sample(int x, int y, EdgeMode mode) =>
        this[EdgePadding.Map(x, Width, mode), EdgePadding.Map(y, Height, mode)];
}
