using Lab3.PixelAccess;

namespace Lab3.Processing;

public sealed class IntegralImage
{
    private readonly long[,] _sums;
    private readonly long[,] _squaredSums;

    public int Width { get; }
    public int Height { get; }
    public int Padding { get; }

    public IntegralImage(GrayBuffer source, int padding = 0, EdgeMode edgeMode = EdgeMode.Clamp,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentOutOfRangeException.ThrowIfNegative(padding);
        if (edgeMode is not (EdgeMode.Clamp or EdgeMode.Reflect))
            throw new ArgumentOutOfRangeException(nameof(edgeMode));
        cancellationToken.ThrowIfCancellationRequested();

        Width = source.Width;
        Height = source.Height;
        Padding = padding;
        var paddedWidth = checked(Width + 2 * padding);
        var paddedHeight = checked(Height + 2 * padding);
        _sums = new long[checked(paddedHeight + 1), checked(paddedWidth + 1)];
        _squaredSums = new long[checked(paddedHeight + 1), checked(paddedWidth + 1)];

        var columns = new int[paddedWidth];
        for (var x = 0; x < paddedWidth; x++)
            columns[x] = EdgePadding.Map(x - padding, Width, edgeMode);

        // Нулевые строка и столбец позволяют одинаково считать окна у границ и внутри.
        for (var y = 0; y < paddedHeight; y++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var sourceY = EdgePadding.Map(y - padding, Height, edgeMode);
            long rowSum = 0;
            long rowSquaredSum = 0;
            for (var x = 0; x < paddedWidth; x++)
            {
                var value = source.Pixels[sourceY, columns[x]];
                rowSum += value;
                rowSquaredSum += value * value;
                _sums[y + 1, x + 1] = _sums[y, x + 1] + rowSum;
                _squaredSums[y + 1, x + 1] = _squaredSums[y, x + 1] + rowSquaredSum;
            }
        }
    }

    // Координаты относятся к исходной картинке. В области padding они могут быть отрицательными.
    public long GetSum(int left, int top, int width, int height) =>
        Query(_sums, left, top, width, height);

    public long GetSquaredSum(int left, int top, int width, int height) =>
        Query(_squaredSums, left, top, width, height);

    public (double Mean, double StandardDeviation) GetStatistics(int left, int top, int width, int height)
    {
        var sum = GetSum(left, top, width, height);
        var squaredSum = GetSquaredSum(left, top, width, height);
        var count = (long)width * height;
        var mean = (double)sum / count;
        var variance = (double)squaredSum / count - mean * mean;
        // После округления double дисперсия однотонного окна может оказаться чуть меньше нуля.
        return (mean, Math.Sqrt(Math.Max(0, variance)));
    }

    public (double Mean, double StandardDeviation) GetWindowStatistics(int x, int y, int radius)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(x);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(x, Width);
        ArgumentOutOfRangeException.ThrowIfNegative(y);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(y, Height);
        ArgumentOutOfRangeException.ThrowIfNegative(radius);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(radius, Padding);
        var size = checked(2 * radius + 1);
        return GetStatistics(x - radius, y - radius, size, size);
    }

    private long Query(long[,] table, int left, int top, int width, int height)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        var x1 = (long)left + Padding;
        var y1 = (long)top + Padding;
        var x2 = x1 + width;
        var y2 = y1 + height;
        if (x1 < 0 || x2 >= table.GetLength(1))
            throw new ArgumentOutOfRangeException(nameof(left), "Окно выходит за подготовленную область.");
        if (y1 < 0 || y2 >= table.GetLength(0))
            throw new ArgumentOutOfRangeException(nameof(top), "Окно выходит за подготовленную область.");

        // Сумма прямоугольника: нижний правый - верхний правый - нижний левый + верхний левый.
        return table[(int)y2, (int)x2] - table[(int)y1, (int)x2]
            - table[(int)y2, (int)x1] + table[(int)y1, (int)x1];
    }
}
