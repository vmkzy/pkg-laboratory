using Lab4.RasterEngine;

internal sealed class TestSuite
{
    public int Count { get; private set; }
    public List<string> Failures { get; } = [];

    public void Check(bool condition, string description)
    {
        Count++;
        if (!condition)
            Failures.Add(description);
    }

    public void Throws<T>(Action action, string description) where T : Exception
    {
        try
        {
            action();
            Check(false, description);
        }
        catch (T)
        {
            Check(true, description);
        }
        catch (Exception exception)
        {
            Check(false, $"{description}: вместо {typeof(T).Name} получено {exception.GetType().Name}");
        }
    }
}

internal readonly record struct Point(int X, int Y);
internal readonly record struct Pixel(int X, int Y, double Intensity);

internal sealed class RecordingTarget : IRasterTarget
{
    public List<Pixel> Pixels { get; } = [];
    public void SetPixel(int x, int y, double intensity) => Pixels.Add(new Pixel(x, y, intensity));
}
