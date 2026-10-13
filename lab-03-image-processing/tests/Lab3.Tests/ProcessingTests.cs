using Lab3.PixelAccess;
using Lab3.Processing;

internal static class ProcessingTests
{
    public static void Run(Action<bool, string> check)
    {
        var gray = new GrayBuffer(11, 9);
        for (var y = 0; y < gray.Height; y++)
            for (var x = 0; x < gray.Width; x++) gray[x, y] = (byte)((x * 37 + y * 61) % 256);
        foreach (var mode in new[] { EdgeMode.Clamp, EdgeMode.Reflect })
        {
            var adaptive = AdaptiveThresholding.Apply(gray, 3, 0.2, mode, false);
            var matches = true;
            for (var y = 0; y < gray.Height; y++)
                for (var x = 0; x < gray.Width; x++)
                {
                    double sum = 0, squared = 0;
                    for (var dy = -3; dy <= 3; dy++)
                        for (var dx = -3; dx <= 3; dx++)
                        {
                            var value = gray.Sample(x + dx, y + dy, mode);
                            sum += value; squared += value * value;
                        }
                    var mean = sum / 49;
                    var threshold = mean * (1 + 0.2 * (Math.Sqrt(Math.Max(0, squared / 49 - mean * mean)) / 128 - 1));
                    matches &= adaptive[x, y] == (gray[x, y] > threshold ? 255 : 0);
                }
            check(matches, $"Саувола совпадает с прямым обходом окон: {mode}");

            var blurred = SpatialFilters.Gaussian(gray, 0.8, mode, false);
            var kernel = SpatialFilters.GaussianKernel(0.8);
            var radius = kernel.Length / 2;
            var maxError = 0.0;
            for (var y = 0; y < gray.Height; y++)
                for (var x = 0; x < gray.Width; x++)
                {
                    double sum = 0;
                    for (var dy = -radius; dy <= radius; dy++)
                        for (var dx = -radius; dx <= radius; dx++)
                            sum += kernel[dx + radius] * kernel[dy + radius] * gray.Sample(x + dx, y + dy, mode);
                    maxError = Math.Max(maxError, Math.Abs(sum - blurred.Pixels[y, x]));
                }
            check(maxError < 1e-10, $"Два прохода Гаусса совпадают с двумерной свёрткой: {mode}");
        }
        check(Math.Abs(SpatialFilters.GaussianKernel(1.2).Sum() - 1) < 1e-12, "Нормировка ядра Гаусса");
        var flat = new GrayBuffer(15, 15);
        for (var y = 0; y < 15; y++)
            for (var x = 0; x < 15; x++) flat[x, y] = 123;
        foreach (var mode in new[] { EdgeMode.Clamp, EdgeMode.Reflect })
        {
            check(SpatialFilters.Detect(flat, true, LineDirection.All, 1, mode).Pixels.Cast<byte>().All(v => v == 0), "Однотонное поле: нет точек и чёрной рамки " + mode);
            check(EdgeDetection.Detect(flat, true, 1.2, 30, 80, mode).Image.Pixels.Cast<byte>().All(v => v == 0), "Кэнни: нет границ однотонного поля " + mode);
        }
        var spot = new GrayBuffer(7, 7); spot[3, 3] = 255;
        var detected = SpatialFilters.Detect(spot, true, LineDirection.All, 1000, EdgeMode.Reflect);
        check(detected[3, 3] == 255 && detected.Pixels.Cast<byte>().Count(v => v != 0) == 1, "Точечная маска выделяет одиночную яркую точку");
        var line = new GrayBuffer(9, 9);
        for (var x = 0; x < 9; x++) line[x, 4] = 255;
        check(SpatialFilters.Detect(line, false, LineDirection.Horizontal, 1000, EdgeMode.Clamp)[4, 4] == 255, "Горизонтальная маска выделяет горизонтальную линию");
        check(SpatialFilters.Detect(line, false, LineDirection.Vertical, 1000, EdgeMode.Clamp)[4, 4] == 0, "Вертикальная маска не выделяет горизонтальную линию");

        var ramp = new DoubleBuffer(9, 9);
        for (var y = 0; y < 9; y++)
            for (var x = 0; x < 9; x++) ramp.Pixels[y, x] = 3 * x + 4 * y;
        var gradient = EdgeDetection.Sobel(ramp, EdgeMode.Reflect);
        check(gradient.X.Pixels[4, 4] == 24 && gradient.Y.Pixels[4, 4] == 32, "Собель: обе составляющие градиента");
        check(Math.Abs(gradient.Angle.Pixels[4, 4] - Math.Atan2(32, 24)) < 1e-12, "Собель: точный atan2 вместо округления направления");
        check(EdgeDetection.DirectionColor(0) == new RgbPixel(255, 0, 0), "Направление 0: красный");
        check(EdgeDetection.DirectionColor(Math.PI) == new RgbPixel(0, 255, 255), "Направление π: голубой");
        check(Math.Abs(ramp.SampleBilinear(2.5, 3.5, EdgeMode.Clamp) - 21.5) < 1e-12, "Билинейная интерполяция дробных координат");

        var response = new DoubleBuffer(8, 6);
        response.Pixels[2, 1] = 100; response.Pixels[2, 2] = 50; response.Pixels[3, 3] = 40;
        response.Pixels[4, 6] = 50; response.Pixels[1, 1] = 10;
        var linked = EdgeDetection.Hysteresis(response, 30, 80);
        check(linked[1, 2] == 255 && linked[2, 2] == 255 && linked[3, 3] == 255, "Гистерезис сохраняет связную цепь слабых границ");
        check(linked[6, 4] == 0 && linked[1, 1] == 0, "Гистерезис удаляет изолированную слабую границу и подпороговый отклик");
        var step = new GrayBuffer(32, 20);
        for (var y = 0; y < 20; y++)
            for (var x = 16; x < 32; x++) step[x, y] = 255;
        var edges = EdgeDetection.Detect(step, true, 1.2, 30, 80, EdgeMode.Reflect);
        check(edges.Points.Count == 20, "Кэнни оставляет границу ступени толщиной один пиксель");
        check(edges.Points.All(p => Math.Abs(p.X - 15.5) < 0.01), "Субпиксельная граница ступени находится между столбцами 15 и 16");
        check(edges.Image[16, 0] == 255 && edges.Image[16, 19] == 255, "Граница продолжается до края, без принудительной чёрной рамки");

        var large = new PixelBuffer(512, 512);
        for (var y = 0; y < 512; y++)
            for (var x = 0; x < 512; x++) large[x, y] = new((byte)(x % 256), (byte)(y % 256), (byte)((x + y) % 256));
        foreach (var method in Enum.GetValues<ProcessingMethod>())
        {
            var settings = new ProcessingSettings { Method = method, Parallel = false };
            var serial = ImageProcessor.Process(large, settings);
            var parallel = ImageProcessor.Process(large, settings with { Parallel = true });
            check(serial.Image.Pixels.Cast<byte>().SequenceEqual(parallel.Image.Pixels.Cast<byte>()), "Параллельный и обычный результат совпадают: " + method);
            check(serial.BeforeHistogram.Sum() == 512L * 512 && serial.AfterHistogram.Sum() == 512L * 512, "Гистограммы сохраняют число пикселей: " + method);
            if (serial.Directions is not null)
                check(serial.Directions.Pixels.Cast<byte>().SequenceEqual(parallel.Directions!.Pixels.Cast<byte>()) && serial.EdgePoints.SequenceEqual(parallel.EdgePoints), "Направления и субпиксельные координаты совпадают: " + method);
        }
        using var canceled = new CancellationTokenSource(); canceled.Cancel();
        check(Throws<OperationCanceledException>(() => ImageProcessor.Process(large, new(), canceled.Token)), "Отмена перед началом обработки");
        check(Throws<OperationCanceledException>(() => AdaptiveThresholding.Apply(gray, 2, .2, EdgeMode.Reflect, true, canceled.Token)), "Отмена интегрального адаптивного порога");
        check(Throws<ArgumentOutOfRangeException>(() => AdaptiveThresholding.Apply(gray, 0, .2, EdgeMode.Clamp)), "Проверка радиуса окна");
        check(Throws<ArgumentOutOfRangeException>(() => AdaptiveThresholding.Apply(gray, 2, double.NaN, EdgeMode.Clamp)), "Проверка k");
        check(Throws<ArgumentOutOfRangeException>(() => SpatialFilters.GaussianKernel(0)), "Проверка σ");
        check(Throws<ArgumentOutOfRangeException>(() => EdgeDetection.Hysteresis(response, 90, 80)), "Проверка двойного порога");
    }

    private static bool Throws<T>(Action action) where T : Exception
    {
        try { action(); return false; }
        catch (T) { return true; }
    }
}
