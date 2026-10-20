using Lab4.RasterEngine;

namespace Lab4.RasterData;

public sealed class VirtualGrid : IRasterTarget
{
    private readonly double[,] _pixels;

    public int Width { get; }
    public int Height { get; }
    public int MinX => -Width / 2;
    public int MaxX => MinX + Width - 1;
    public int MaxY => Height / 2;
    public int MinY => MaxY - Height + 1;

    public VirtualGrid(int width, int height)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        Width = width;
        Height = height;
        _pixels = new double[height, width];
    }

    public double this[int x, int y]
    {
        get
        {
            if (!Contains(x, y))
                throw new ArgumentOutOfRangeException(nameof(x), "Координаты находятся вне растра.");
            return _pixels[MaxY - y, x - MinX];
        }
    }

    public bool Contains(int x, int y) => x >= MinX && x <= MaxX && y >= MinY && y <= MaxY;

    public void SetPixel(int x, int y, double intensity)
    {
        if (!double.IsFinite(intensity) || intensity < 0 || intensity > 1)
            throw new ArgumentOutOfRangeException(nameof(intensity));
        if (!Contains(x, y))
            return;

        // Интенсивности храним в диапазоне 0..1. Повторная запись не ослабляет пиксель.
        ref var pixel = ref _pixels[MaxY - y, x - MinX];
        pixel = Math.Max(pixel, intensity);
    }

    public void Clear() => Array.Clear(_pixels);

    public int CountSetPixels()
    {
        var count = 0;
        foreach (var pixel in _pixels)
            if (pixel > 0)
                count++;
        return count;
    }
}
