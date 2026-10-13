using Lab3.PixelAccess;
using Lab3.Processing;

internal static class IntegralImageTests
{
    public static void Run(Action<bool, string> check)
    {
        var image = new GrayBuffer(3, 3);
        for (var y = 0; y < image.Height; y++)
        for (var x = 0; x < image.Width; x++)
            image[x, y] = (byte)(y * 3 + x + 1);
        var savedPixels = image.Pixels.Cast<byte>().ToArray();
        var integral = new IntegralImage(image);

        check(integral.GetSum(0, 0, 3, 3) == 45 && integral.GetSquaredSum(0, 0, 3, 3) == 285,
            "Суммы яркостей 1..9 и их квадратов совпадают с ручным расчётом");
        check(integral.GetSum(1, 1, 2, 2) == 28 && integral.GetSquaredSum(1, 1, 2, 2) == 206,
            "Для прямоугольника [5, 6, 8, 9] суммы равны 28 и 206");
        var statistics = integral.GetStatistics(1, 1, 2, 2);
        check(Math.Abs(statistics.Mean - 7) < 1e-10 &&
              Math.Abs(statistics.StandardDeviation - Math.Sqrt(2.5)) < 1e-10,
            "Среднее и стандартное отклонение окна рассчитаны по суммам");

        var clamp = new IntegralImage(image, 2, EdgeMode.Clamp);
        var reflect = new IntegralImage(image, 2, EdgeMode.Reflect);
        check(clamp.GetSum(-2, -2, 5, 5) == 85 && reflect.GetSum(-2, -2, 5, 5) == 105,
            "Окно у верхнего левого угла использует выбранный режим границ");
        check(Math.Abs(clamp.GetWindowStatistics(0, 0, 2).Mean - 3.4) < 1e-10 &&
              Math.Abs(reflect.GetWindowStatistics(0, 0, 2).Mean - 4.2) < 1e-10,
            "Полное окно 5x5 сохраняется у края вместо обрезания");
        check(clamp.GetWindowStatistics(2, 1, 0) == (6, 0),
            "Окно радиуса 0 содержит один пиксель с нулевым отклонением");

        foreach (var mode in Enum.GetValues<EdgeMode>())
            CheckAgainstDirectSum(mode, check);

        var single = new GrayBuffer(1, 1);
        single[0, 0] = 100;
        var singleIntegral = new IntegralImage(single, 5, EdgeMode.Reflect);
        check(singleIntegral.GetSum(-5, -5, 11, 11) == 12_100 &&
              singleIntegral.GetWindowStatistics(0, 0, 5) == (100, 0),
            "Отражение одного пикселя работает и для окна больше изображения");

        var white = new GrayBuffer(1024, 1024);
        for (var y = 0; y < white.Height; y++)
        for (var x = 0; x < white.Width; x++)
            white[x, y] = 255;
        var whiteIntegral = new IntegralImage(white);
        check(whiteIntegral.GetSquaredSum(0, 0, 1024, 1024) == 65_025L * 1024 * 1024 &&
              whiteIntegral.GetStatistics(0, 0, 1024, 1024) == (255, 0),
            "Сумма квадратов больше int.MaxValue не переполняется");
        check(image.Pixels.Cast<byte>().SequenceEqual(savedPixels),
            "Построение интегрального изображения сохраняет исходные яркости");

        check(Throws<ArgumentOutOfRangeException>(() => new IntegralImage(image, -1)) &&
              Throws<ArgumentOutOfRangeException>(() => new IntegralImage(image, 0, (EdgeMode)10)),
            "Параметры расширения границ проверяются до построения таблиц");
        check(Throws<ArgumentOutOfRangeException>(() => integral.GetSum(-1, 0, 1, 1)) &&
              Throws<ArgumentOutOfRangeException>(() => integral.GetSum(0, 0, 4, 1)) &&
              Throws<ArgumentOutOfRangeException>(() => integral.GetSum(0, 0, 0, 1)) &&
              Throws<ArgumentOutOfRangeException>(() => clamp.GetWindowStatistics(0, 0, 3)),
            "Окно не может выйти за область, для которой построены суммы");
        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        check(Throws<OperationCanceledException>(() => new IntegralImage(image, 0,
                EdgeMode.Clamp, canceled.Token)), "Отменённое построение таблиц завершается сразу");
    }

    private static void CheckAgainstDirectSum(EdgeMode mode, Action<bool, string> check)
    {
        var image = new GrayBuffer(7, 5);
        for (var y = 0; y < image.Height; y++)
        for (var x = 0; x < image.Width; x++)
            image[x, y] = (byte)((x * 37 + y * 53) % 256);
        var integral = new IntegralImage(image, 2, mode);
        var correct = true;
        for (var radius = 0; radius <= 2; radius++)
        for (var y = 0; y < image.Height; y++)
        for (var x = 0; x < image.Width; x++)
        {
            long sum = 0;
            long squaredSum = 0;
            for (var dy = -radius; dy <= radius; dy++)
            for (var dx = -radius; dx <= radius; dx++)
            {
                var value = image.Sample(x + dx, y + dy, mode);
                sum += value;
                squaredSum += value * value;
            }
            var size = 2 * radius + 1;
            var actual = integral.GetWindowStatistics(x, y, radius);
            var mean = (double)sum / (size * size);
            var deviation = Math.Sqrt(Math.Max(0, (double)squaredSum / (size * size) - mean * mean));
            correct &= integral.GetSum(x - radius, y - radius, size, size) == sum &&
                       integral.GetSquaredSum(x - radius, y - radius, size, size) == squaredSum &&
                       Math.Abs(actual.Mean - mean) < 1e-10 &&
                       Math.Abs(actual.StandardDeviation - deviation) < 1e-10;
        }
        check(correct, $"Все окна радиуса 0..2 совпадают с прямым суммированием: {mode}");
    }

    private static bool Throws<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return true; }
        return false;
    }
}
