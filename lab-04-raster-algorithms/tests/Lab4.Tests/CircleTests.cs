using Lab4.RasterEngine;

internal static class CircleTests
{
    public static void Run(TestSuite suite)
    {
        for (var radius = 0; radius <= 60; radius++)
        {
            Validate(suite, 0, 0, radius);
            Validate(suite, -17, 9, radius);
        }
        Validate(suite, int.MaxValue, int.MinValue, 0);
        Validate(suite, int.MaxValue - 10, int.MinValue + 10, 10);
        suite.Throws<ArgumentOutOfRangeException>(() =>
            RasterAlgorithms.BresenhamCircle(0, 0, -1, new RecordingTarget()), "Отрицательный радиус");
        suite.Throws<ArgumentOutOfRangeException>(() =>
            RasterAlgorithms.BresenhamCircle(int.MaxValue, 0, 1, new RecordingTarget()), "Переполнение центра X");
        suite.Throws<ArgumentOutOfRangeException>(() =>
            RasterAlgorithms.BresenhamCircle(0, int.MinValue, 1, new RecordingTarget()), "Переполнение центра Y");
        suite.Throws<ArgumentNullException>(() =>
            RasterAlgorithms.BresenhamCircle(0, 0, 1, null!), "Окружность: пустой target");
    }

    private static void Validate(TestSuite suite, int centerX, int centerY, int radius)
    {
        var target = new RecordingTarget();
        RasterAlgorithms.BresenhamCircle(centerX, centerY, radius, target);
        var actual = target.Pixels.Select(pixel => new Point(pixel.X, pixel.Y)).ToHashSet();
        var expected = new HashSet<Point>();
        for (var x = 0; x <= radius; x++)
        {
            // Независимый эталон через уравнение окружности, только для тестов.
            var y = (int)Math.Round(Math.Sqrt((double)radius * radius - (double)x * x),
                MidpointRounding.AwayFromZero);
            if (x > y)
                break;
            foreach (var (offsetX, offsetY) in new[]
                { (x, y), (-x, y), (x, -y), (-x, -y), (y, x), (-y, x), (y, -x), (-y, -x) })
                expected.Add(new Point(centerX + offsetX, centerY + offsetY));
        }

        var description = $"Окружность ({centerX},{centerY}), R={radius}";
        suite.Check(actual.SetEquals(expected), description + ": совпадение с независимым эталоном");
        suite.Check(actual.Count == target.Pixels.Count && target.Pixels.All(pixel => pixel.Intensity == 1),
            description + ": интенсивность и отсутствие повторных записей");
        suite.Check(actual.All(point =>
        {
            var dx = (long)point.X - centerX;
            var dy = (long)point.Y - centerY;
            return Math.Abs(Math.Sqrt((double)dx * dx + (double)dy * dy) - radius) <= 0.5;
        }), description + ": радиальная погрешность до половины пикселя");

        var connected = new HashSet<Point>();
        var queue = new Queue<Point>();
        queue.Enqueue(actual.First());
        connected.Add(actual.First());
        while (queue.Count > 0)
        {
            var point = queue.Dequeue();
            foreach (var next in actual.Where(next =>
                Math.Abs((long)next.X - point.X) <= 1 && Math.Abs((long)next.Y - point.Y) <= 1))
                if (connected.Add(next))
                    queue.Enqueue(next);
        }
        suite.Check(connected.Count == actual.Count, description + ": связность контура по 8 соседям");
    }
}
