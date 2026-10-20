using Lab4.RasterEngine;

internal static class LineTests
{
    private delegate void LineAlgorithm(int x0, int y0, int x1, int y1, IRasterTarget target);

    public static void Run(TestSuite suite)
    {
        (string Name, LineAlgorithm Draw)[] algorithms =
        [
            ("Пошаговый", RasterAlgorithms.StepLine),
            ("ЦДА", RasterAlgorithms.DdaLine),
            ("Брезенхем", RasterAlgorithms.BresenhamLine)
        ];
        Point[] endpoints =
        [
            new(7, 3), new(3, 7), new(-3, 7), new(-7, 3),
            new(-7, -3), new(-3, -7), new(3, -7), new(7, -3),
            new(8, 0), new(0, 8), new(-8, 0), new(0, -8),
            new(8, 8), new(-8, 8), new(-8, -8), new(8, -8),
            new(4, 2), new(-4, 2), new(-4, -2), new(4, -2), new(0, 0)
        ];
        Point[] expected = [new(0, 0), new(1, 1), new(2, 1), new(3, 2), new(4, 2), new(5, 3)];
        foreach (var (name, draw) in algorithms)
        {
            var example = new RecordingTarget();
            draw(0, 0, 5, 3, example);
            suite.Check(example.Pixels.Select(pixel => new Point(pixel.X, pixel.Y)).SequenceEqual(expected),
                $"{name}: известный пример (0,0)–(5,3)");

            foreach (var endpoint in endpoints)
            {
                Validate(suite, name, draw, new(0, 0), endpoint);
                Validate(suite, name, draw, endpoint, new(0, 0));
                Validate(suite, name, draw, new(-13, 11), new(endpoint.X - 13, endpoint.Y + 11));
            }

            var random = new Random(9);
            for (var i = 0; i < 500; i++)
                Validate(suite, name, draw, new(random.Next(-60, 61), random.Next(-60, 61)),
                    new(random.Next(-60, 61), random.Next(-60, 61)));
            Validate(suite, name, draw, new(int.MinValue, int.MinValue), new(int.MinValue + 7, int.MinValue + 3));
            Validate(suite, name, draw, new(int.MaxValue, int.MaxValue), new(int.MaxValue - 3, int.MaxValue - 7));
            Validate(suite, name, draw, new(-50_000, 0), new(50_000, 37));
            suite.Throws<ArgumentNullException>(() => draw(0, 0, 1, 1, null!), $"{name}: пустой target");
        }
    }

    private static void Validate(TestSuite suite, string name, LineAlgorithm draw, Point start, Point end)
    {
        var target = new RecordingTarget();
        draw(start.X, start.Y, end.X, end.Y, target);
        var dx = (long)end.X - start.X;
        var dy = (long)end.Y - start.Y;
        var steps = Math.Max(Math.Abs(dx), Math.Abs(dy));
        var pixels = target.Pixels;
        var description = $"{name}: ({start.X},{start.Y})–({end.X},{end.Y})";
        suite.Check(pixels.Count == steps + 1, description + ", число пикселей");
        suite.Check(pixels.Count > 0 && pixels[0].X == start.X && pixels[0].Y == start.Y &&
            pixels[^1].X == end.X && pixels[^1].Y == end.Y, description + ", оба конца");

        var valid = true;
        for (var i = 0; i < pixels.Count; i++)
        {
            var pixel = pixels[i];
            valid &= pixel.Intensity == 1 && pixel.X >= Math.Min(start.X, end.X) &&
                pixel.X <= Math.Max(start.X, end.X) && pixel.Y >= Math.Min(start.Y, end.Y) &&
                pixel.Y <= Math.Max(start.Y, end.Y);
            var localX = (long)pixel.X - start.X;
            var localY = (long)pixel.Y - start.Y;
            if (steps > 0)
            {
                // Проверяем геометрию, а не совпадение с другим алгоритмом:
                // при точном попадании между клетками допустимы оба ближайших пикселя.
                var error = Math.Abs(dx) >= Math.Abs(dy)
                    ? Math.Abs(localY - localX * ((double)dy / dx))
                    : Math.Abs(localX - localY * ((double)dx / dy));
                valid &= error <= 0.50001;
            }
            if (i > 0)
            {
                var previous = pixels[i - 1];
                var moveX = Math.Abs((long)pixel.X - previous.X);
                var moveY = Math.Abs((long)pixel.Y - previous.Y);
                valid &= moveX <= 1 && moveY <= 1 && moveX + moveY > 0;
            }
        }
        suite.Check(valid, description + ", непрерывность, границы и погрешность до половины пикселя");
    }
}
