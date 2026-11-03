namespace Lab4.RasterData;

public readonly record struct LineSegment(int X0, int Y0, int X1, int Y1);
public readonly record struct Circle(int X, int Y, int Radius);

public static class BenchmarkWorkloads
{
    // Концы веера равномерно распределены по восьми октантам; оси не дублируются.
    public static LineSegment[] CreateLines(int count, int maximumLength, int seed, bool fixedLength = false)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);
        ArgumentOutOfRangeException.ThrowIfLessThan(maximumLength, 2);
        var random = new Random(seed);
        var lines = new LineSegment[count];
        for (var i = 0; i < count; i++)
        {
            var major = fixedLength ? maximumLength : random.Next(2, maximumLength + 1);
            var minor = random.Next(1, major);
            var (x, y) = (i % 8) switch
            {
                0 => (major, minor),
                1 => (minor, major),
                2 => (-minor, major),
                3 => (-major, minor),
                4 => (-major, -minor),
                5 => (-minor, -major),
                6 => (minor, -major),
                _ => (major, -minor)
            };
            lines[i] = new LineSegment(0, 0, x, y);
        }
        return lines;
    }

    public static Circle[] CreateCircles(int count, int maximumRadius, int seed)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumRadius);
        var random = new Random(seed);
        var circles = new Circle[count];
        for (var i = 0; i < count; i++)
            circles[i] = new Circle(0, 0, random.Next(1, maximumRadius + 1));
        return circles;
    }
}
