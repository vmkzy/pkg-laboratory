namespace Lab4.RasterEngine;

public static partial class RasterAlgorithms
{
    public static void MidpointEllipse(int centerX, int centerY, int radiusX, int radiusY, IRasterTarget target)
    {
        ArgumentNullException.ThrowIfNull(target);
        if (radiusX < 0 || (long)centerX - radiusX < int.MinValue || (long)centerX + radiusX > int.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(radiusX));
        if (radiusY < 0 || (long)centerY - radiusY < int.MinValue || (long)centerY + radiusY > int.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(radiusY));
        if (radiusX == 0 || radiusY == 0)
        {
            BresenhamLine(centerX - radiusX, centerY - radiusY, centerX + radiusX, centerY + radiusY, target);
            return;
        }

        // Большая полуось становится ведущей: это важно у очень узких эллипсов.
        var transpose = radiusX < radiusY;
        if (transpose)
            (radiusX, radiusY) = (radiusY, radiusX);

        // Решения умножены на 4, поэтому даже четверти в формулах остаются целыми.
        // Int128 нужен для произведений квадратов полуосей без переполнения long.
        Int128 a2 = (Int128)radiusX * radiusX;
        Int128 b2 = (Int128)radiusY * radiusY;
        long x = 0;
        long y = radiusY;
        Int128 changeX = 0;
        var changeY = 2 * a2 * y;
        var decision = 4 * b2 - 4 * a2 * y + a2;
        while (changeX < changeY)
        {
            PlotEllipsePoint(centerX, centerY, x, y, transpose, target);
            x++;
            changeX += 2 * b2;
            if (decision < 0)
                decision += 4 * changeX + 4 * b2;
            else
            {
                y--;
                changeY -= 2 * a2;
                decision += 4 * changeX - 4 * changeY + 4 * b2;
            }
        }

        decision = b2 * (2 * x + 1) * (2 * x + 1) +
            4 * a2 * (y - 1) * (y - 1) - 4 * a2 * b2;
        while (y > 0)
        {
            PlotEllipsePoint(centerX, centerY, x, y, transpose, target);
            y--;
            changeY -= 2 * a2;
            if (decision > 0)
                decision += 4 * a2 - 4 * changeY;
            else
            {
                x++;
                changeX += 2 * b2;
                decision += 4 * changeX - 4 * changeY + 4 * a2;
            }
        }

        // У очень плоского эллипса несколько последних пикселей лежат на оси X.
        for (; x <= radiusX; x++)
            PlotEllipsePoint(centerX, centerY, x, 0, transpose, target);
    }

    private static void PlotEllipsePoint(int centerX, int centerY, long x, long y, bool transpose,
        IRasterTarget target) => PlotFour(centerX, centerY, transpose ? y : x, transpose ? x : y, target);
}
