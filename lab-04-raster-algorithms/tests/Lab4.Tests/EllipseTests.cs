using Lab4.RasterEngine;

internal static class EllipseTests
{
    public static void Run(TestSuite suite)
    {
        for (var a = 0; a <= 24; a++)
            for (var b = 0; b <= 24; b++)
                Validate(suite, -7, 9, a, b);
        Validate(suite, 0, 0, 40, 1);
        Validate(suite, 0, 0, 1, 40);
        Validate(suite, int.MaxValue - 7, int.MinValue + 3, 7, 3);
        suite.Throws<ArgumentOutOfRangeException>(() =>
            RasterAlgorithms.MidpointEllipse(0, 0, -1, 2, new RecordingTarget()), "Эллипс: отрицательная полуось");
        suite.Throws<ArgumentOutOfRangeException>(() =>
            RasterAlgorithms.MidpointEllipse(int.MaxValue, 0, 1, 2, new RecordingTarget()), "Эллипс: переполнение X");
        suite.Throws<ArgumentOutOfRangeException>(() =>
            RasterAlgorithms.MidpointEllipse(0, int.MinValue, 2, 1, new RecordingTarget()), "Эллипс: переполнение Y");
        suite.Throws<ArgumentNullException>(() =>
            RasterAlgorithms.MidpointEllipse(0, 0, 2, 1, null!), "Эллипс: пустой target");
    }

    private static void Validate(TestSuite suite, int centerX, int centerY, int a, int b)
    {
        var target = new RecordingTarget();
        RasterAlgorithms.MidpointEllipse(centerX, centerY, a, b, target);
        var actual = target.Pixels.Select(pixel => new Point(pixel.X, pixel.Y)).ToHashSet();
        var expected = Reference(centerX, centerY, a, b);
        var description = $"Эллипс A={a}, B={b}";
        suite.Check(actual.SetEquals(expected), description + ": прямое вычисление функции в серединах клеток");
        suite.Check(target.Pixels.Count == actual.Count && target.Pixels.All(pixel => pixel.Intensity == 1),
            description + ": интенсивность и отсутствие повторов");
        suite.Check(actual.Contains(new(centerX + a, centerY)) && actual.Contains(new(centerX - a, centerY)) &&
            actual.Contains(new(centerX, centerY + b)) && actual.Contains(new(centerX, centerY - b)),
            description + ": четыре вершины, включая плоские эллипсы");
        if (a > 0 && b > 0)
            suite.Check(actual.All(point =>
            {
                var x = Math.Abs((long)point.X - centerX);
                var y = Math.Abs((long)point.Y - centerY);
                // Проверяем пересечение кривой с квадратом пикселя через знаки F в его углах.
                // Проекция ошибки только на X или Y неверна возле перехода между областями.
                decimal lowX = Math.Max(0, x - 0.5m);
                decimal lowY = Math.Max(0, y - 0.5m);
                decimal highX = x + 0.5m;
                decimal highY = y + 0.5m;
                decimal a2 = (decimal)a * a;
                decimal b2 = (decimal)b * b;
                var low = b2 * lowX * lowX + a2 * lowY * lowY - a2 * b2;
                var high = b2 * highX * highX + a2 * highY * highY - a2 * b2;
                return x <= a && y <= b && low <= 0 && high >= 0;
            }), description + ": эллипс пересекает квадрат каждого выбранного пикселя");
        var visited = new HashSet<Point> { actual.First() };
        var queue = new Queue<Point>(visited);
        while (queue.Count > 0)
        {
            var point = queue.Dequeue();
            for (var dy = -1; dy <= 1; dy++)
                for (var dx = -1; dx <= 1; dx++)
                {
                    var x = (long)point.X + dx;
                    var y = (long)point.Y + dy;
                    if (x < int.MinValue || x > int.MaxValue || y < int.MinValue || y > int.MaxValue)
                        continue;
                    var next = new Point((int)x, (int)y);
                    if (actual.Contains(next) && visited.Add(next))
                        queue.Enqueue(next);
                }
        }
        suite.Check(visited.Count == actual.Count, description + ": связность контура по 8 соседям");
        if (a == b)
        {
            var circle = new RecordingTarget();
            RasterAlgorithms.BresenhamCircle(centerX, centerY, a, circle);
            suite.Check(actual.SetEquals(circle.Pixels.Select(pixel => new Point(pixel.X, pixel.Y))),
                description + ": равные полуоси дают окружность");
        }
    }

    private static HashSet<Point> Reference(int centerX, int centerY, int a, int b)
    {
        var points = new HashSet<Point>();
        if (a == 0 || b == 0)
        {
            for (var x = -a; x <= a; x++)
                for (var y = -b; y <= b; y++)
                    points.Add(new(centerX + x, centerY + y));
            return points;
        }

        var transpose = a < b;
        if (transpose)
            (a, b) = (b, a);
        decimal a2 = (decimal)a * a;
        decimal b2 = (decimal)b * b;
        var offsetX = 0;
        var offsetY = b;
        while (b2 * offsetX < a2 * offsetY)
        {
            Plot();
            var decision = Function(offsetX + 1, offsetY - 0.5m);
            offsetX++;
            if (decision >= 0)
                offsetY--;
        }
        while (offsetY > 0)
        {
            Plot();
            var decision = Function(offsetX + 0.5m, offsetY - 1);
            offsetY--;
            if (decision <= 0)
                offsetX++;
        }
        for (; offsetX <= a; offsetX++)
            Plot();
        return points;

        decimal Function(decimal x, decimal y) => b2 * x * x + a2 * y * y - a2 * b2;
        void Plot()
        {
            var x = transpose ? offsetY : offsetX;
            var y = transpose ? offsetX : offsetY;
            points.Add(new(centerX + x, centerY + y));
            points.Add(new(centerX - x, centerY + y));
            points.Add(new(centerX + x, centerY - y));
            points.Add(new(centerX - x, centerY - y));
        }
    }
}
