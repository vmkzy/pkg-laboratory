namespace Lab3.PixelAccess;

public static class EdgePadding
{
    public static int Map(int coordinate, int length, EdgeMode mode)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(length);

        return mode switch
        {
            EdgeMode.Clamp => Math.Clamp(coordinate, 0, length - 1),
            EdgeMode.Reflect => Reflect(coordinate, length),
            _ => throw new ArgumentOutOfRangeException(nameof(mode))
        };
    }

    private static int Reflect(int coordinate, int length)
    {
        // Для [a b c d] продолжение выглядит так: ... d c b a | a b c d | d c b a ...
        var period = 2L * length;
        var position = coordinate % period;
        if (position < 0)
            position += period;

        return (int)(position < length ? position : period - position - 1);
    }
}
