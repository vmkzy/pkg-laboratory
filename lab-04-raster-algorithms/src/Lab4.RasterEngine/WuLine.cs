namespace Lab4.RasterEngine;

public static partial class RasterAlgorithms
{
    public static void WuLine(int x0, int y0, int x1, int y1, IRasterTarget target)
    {
        ArgumentNullException.ThrowIfNull(target);
        if (x0 == x1 && y0 == y1)
        {
            target.SetPixel(x0, y0, 1);
            return;
        }

        var steep = Math.Abs((long)y1 - y0) > Math.Abs((long)x1 - x0);
        if (steep)
        {
            (x0, y0) = (y0, x0);
            (x1, y1) = (y1, x1);
        }
        if (x0 > x1)
        {
            (x0, x1) = (x1, x0);
            (y0, y1) = (y1, y0);
        }

        var span = (long)x1 - x0;
        var rise = (long)y1 - y0;
        // При целых координатах концов покрытие крайних столбцов равно 1/2.
        PlotWu(x0, y0, 0.5, steep, target);
        PlotWu(x1, y1, 0.5, steep, target);
        long lower = y0;
        long remainder = 0;
        for (long x = (long)x0 + 1; x < x1; x++)
        {
            // Остаток хранит дробную часть точно, без накопления ошибок double.
            remainder += rise;
            if (remainder >= span) { lower++; remainder -= span; }
            else if (remainder < 0) { lower--; remainder += span; }
            var fraction = (double)remainder / span;
            PlotWu(x, lower, 1 - fraction, steep, target);
            PlotWu(x, lower + 1, fraction, steep, target);
        }
    }

    private static void PlotWu(long x, long y, double intensity, bool steep, IRasterTarget target)
    {
        if (intensity <= 0)
            return;
        if (steep)
            target.SetPixel(checked((int)y), checked((int)x), intensity);
        else
            target.SetPixel(checked((int)x), checked((int)y), intensity);
    }
}
