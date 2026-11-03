using Lab4.RasterData;
using Lab4.RasterEngine;

internal static class WuTests
{
    public static void Run(TestSuite suite)
    {
        Point[] endpoints =
        [
            new(7, 3), new(3, 7), new(-3, 7), new(-7, 3), new(-7, -3), new(-3, -7), new(3, -7), new(7, -3),
            new(0, 0), new(8, 0), new(0, 8), new(-8, 0), new(0, -8), new(8, 8), new(1, 0), new(1, 1)
        ];
        foreach (var point in endpoints)
        {
            Validate(suite, new(0, 0), point);
            Validate(suite, new(-11, -7), new(point.X - 11, point.Y - 7));
        }
        var random = new Random(9);
        for (var i = 0; i < 250; i++)
            Validate(suite, new(random.Next(-30, 31), random.Next(-30, 31)),
                new(random.Next(-30, 31), random.Next(-30, 31)));
        Validate(suite, new(int.MinValue, int.MaxValue), new(int.MinValue + 7, int.MaxValue - 3));
        Validate(suite, new(int.MaxValue, int.MinValue), new(int.MaxValue - 3, int.MinValue + 7));

        var grid = new VirtualGrid(9, 9);
        RasterAlgorithms.WuLine(0, 0, 4, 1, grid);
        suite.Check(grid[1, 0] == 0.75 && grid[1, 1] == 0.25,
            "Ву: дробная часть 0,25 распределена как 0,75 и 0,25");
        suite.Check(grid[0, 0] == 0.5 && grid[4, 1] == 0.5, "Ву: полупокрытие концов отрезка");
        suite.Throws<ArgumentNullException>(() => RasterAlgorithms.WuLine(0, 0, 1, 1, null!), "Ву: пустой target");
    }

    private static void Validate(TestSuite suite, Point start, Point end)
    {
        var actual = new RecordingTarget();
        RasterAlgorithms.WuLine(start.X, start.Y, end.X, end.Y, actual);
        var expected = Reference(start, end);
        var description = $"Ву ({start.X},{start.Y})–({end.X},{end.Y})";
        var values = actual.Pixels.ToDictionary(pixel => new Point(pixel.X, pixel.Y), pixel => pixel.Intensity);
        suite.Check(values.Count == expected.Count && expected.All(pair =>
            values.TryGetValue(pair.Key, out var intensity) && Math.Abs(intensity - pair.Value) < 1e-12),
            description + ": интенсивности совпадают с прямым расчётом decimal");
        suite.Check(values.Values.All(value => double.IsFinite(value) && value > 0 && value <= 1),
            description + ": допустимые интенсивности");
        var reverse = new RecordingTarget();
        RasterAlgorithms.WuLine(end.X, end.Y, start.X, start.Y, reverse);
        suite.Check(reverse.Pixels.Count == values.Count && reverse.Pixels.All(pixel =>
            values.TryGetValue(new Point(pixel.X, pixel.Y), out var intensity) && intensity == pixel.Intensity),
            description + ": разворот не меняет результат");
    }

    private static Dictionary<Point, double> Reference(Point start, Point end)
    {
        if (start == end)
            return new() { [start] = 1 };
        var steep = Math.Abs((long)end.Y - start.Y) > Math.Abs((long)end.X - start.X);
        var first = steep ? new Point(start.Y, start.X) : start;
        var last = steep ? new Point(end.Y, end.X) : end;
        if (first.X > last.X)
            (first, last) = (last, first);
        var result = new Dictionary<Point, double>();
        for (long x = first.X; x <= last.X; x++)
        {
            var y = first.Y + ((decimal)last.Y - first.Y) * (x - first.X) / ((decimal)last.X - first.X);
            var lower = (long)decimal.Floor(y);
            var fraction = y - lower;
            var coverage = x == first.X || x == last.X ? 0.5m : 1m;
            Add(x, lower, coverage * (1 - fraction));
            Add(x, lower + 1, coverage * fraction);
        }
        return result;

        void Add(long x, long y, decimal intensity)
        {
            if (intensity <= 0)
                return;
            result.Add(steep ? new Point((int)y, (int)x) : new Point((int)x, (int)y), (double)intensity);
        }
    }
}
