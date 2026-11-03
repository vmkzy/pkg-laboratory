namespace Lab4.RasterEngine;

public static partial class RasterAlgorithms
{
    public static void StepLine(int x0, int y0, int x1, int y1, IRasterTarget target)
    {
        ArgumentNullException.ThrowIfNull(target);
        var dx = (long)x1 - x0;
        var dy = (long)y1 - y0;
        if (dx == 0 && dy == 0)
        {
            target.SetPixel(x0, y0, 1);
            return;
        }

        // По ведущей координате идём с шагом 1, вторую каждый раз считаем по уравнению прямой.
        if (Math.Abs(dx) >= Math.Abs(dy))
        {
            var slope = (double)dy / dx;
            var direction = Math.Sign(dx);
            for (long i = 0; i <= Math.Abs(dx); i++)
            {
                var x = x0 + i * direction;
                var y = y0 + slope * (x - x0);
                target.SetPixel((int)x, Round(y), 1);
            }
        }
        else
        {
            var slope = (double)dx / dy;
            var direction = Math.Sign(dy);
            for (long i = 0; i <= Math.Abs(dy); i++)
            {
                var y = y0 + i * direction;
                var x = x0 + slope * (y - y0);
                target.SetPixel(Round(x), (int)y, 1);
            }
        }
    }

    public static void DdaLine(int x0, int y0, int x1, int y1, IRasterTarget target)
    {
        ArgumentNullException.ThrowIfNull(target);
        var dx = (long)x1 - x0;
        var dy = (long)y1 - y0;
        var steps = Math.Max(Math.Abs(dx), Math.Abs(dy));
        if (steps == 0)
        {
            target.SetPixel(x0, y0, 1);
            return;
        }

        var incrementX = (double)dx / steps;
        var incrementY = (double)dy / steps;
        double x = x0;
        double y = y0;
        for (long i = 0; i < steps; i++)
        {
            target.SetPixel(Round(x), Round(y), 1);
            x += incrementX;
            y += incrementY;
        }

        // Последний конец задаём точно, без накопленной погрешности double.
        target.SetPixel(x1, y1, 1);
    }

    public static void BresenhamLine(int x0, int y0, int x1, int y1, IRasterTarget target)
    {
        ArgumentNullException.ThrowIfNull(target);
        var dx = Math.Abs((long)x1 - x0);
        var dy = -Math.Abs((long)y1 - y0);
        var stepX = x0 < x1 ? 1 : -1;
        var stepY = y0 < y1 ? 1 : -1;
        var error = dx + dy;
        long x = x0;
        long y = y0;

        while (true)
        {
            target.SetPixel((int)x, (int)y, 1);
            if (x == x1 && y == y1)
                break;

            var doubledError = 2 * error;
            if (doubledError >= dy)
            {
                error += dy;
                x += stepX;
            }
            if (doubledError <= dx)
            {
                error += dx;
                y += stepY;
            }
        }
    }

    public static void BresenhamCircle(int centerX, int centerY, int radius, IRasterTarget target)
    {
        ArgumentNullException.ThrowIfNull(target);
        if (radius < 0 || (long)centerX - radius < int.MinValue ||
            (long)centerX + radius > int.MaxValue || (long)centerY - radius < int.MinValue ||
            (long)centerY + radius > int.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(radius));

        long x = 0;
        long y = radius;
        long decision = 3 - 2L * radius;
        while (x <= y)
        {
            PlotFour(centerX, centerY, x, y, target);
            if (x != y)
                PlotFour(centerX, centerY, y, x, target);

            if (decision < 0)
                decision += 4 * x + 6;
            else
            {
                decision += 4 * (x - y) + 10;
                y--;
            }
            x++;
        }
    }

    private static void PlotFour(int centerX, int centerY, long x, long y, IRasterTarget target)
    {
        target.SetPixel((int)(centerX + x), (int)(centerY + y), 1);
        if (x != 0)
            target.SetPixel((int)(centerX - x), (int)(centerY + y), 1);
        if (y != 0)
        {
            target.SetPixel((int)(centerX + x), (int)(centerY - y), 1);
            if (x != 0)
                target.SetPixel((int)(centerX - x), (int)(centerY - y), 1);
        }
    }

    private static int Round(double value) =>
        checked((int)Math.Round(value, MidpointRounding.AwayFromZero));
}
