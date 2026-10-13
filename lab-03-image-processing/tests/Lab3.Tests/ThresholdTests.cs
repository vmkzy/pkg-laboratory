using Lab3.PixelAccess;
using Lab3.Processing;

internal static class ThresholdTests
{
    public static void Run(Action<bool, string> check)
    {
        var colors = new PixelBuffer(3, 2);
        colors[0, 0] = new RgbPixel(255, 0, 0);
        colors[1, 0] = new RgbPixel(0, 255, 0);
        colors[2, 0] = new RgbPixel(0, 0, 255);
        colors[0, 1] = new RgbPixel(0, 0, 0);
        colors[1, 1] = new RgbPixel(255, 255, 255);
        colors[2, 1] = new RgbPixel(10, 20, 30);
        var savedColors = colors.Pixels.Cast<byte>().ToArray();
        var gray = GrayscaleConverter.Convert(colors);
        check(gray.Pixels.Cast<byte>().SequenceEqual(new byte[] { 76, 150, 29, 0, 255, 18 }),
            "Яркости цветных пикселей рассчитаны вручную с округлением");
        var histogram = ImageHistogram.Build(gray);
        check(histogram.Sum() == 6 && new[] { 0, 18, 29, 76, 150, 255 }.All(value => histogram[value] == 1),
            "Гистограмма учитывает каждый пиксель по его яркости");

        var source = MakeRgb([10, 10, 12, 12, 200, 200, 202, 202], 4);
        var otsu = GlobalThresholding.Apply(source, ThresholdMethod.Otsu);
        var iterative = GlobalThresholding.Apply(source, ThresholdMethod.Iterative);
        check(otsu.Threshold == 12, "Оцу оставляет первый максимум между двумя группами яркостей");
        check(iterative.Threshold == 106, "Итеративный порог равен среднему двух средних: (11 + 201) / 2");
        check(otsu.Image.Width == 4 && otsu.Image.Height == 2 &&
              otsu.Image.Pixels.Cast<byte>().SequenceEqual(new byte[] { 0, 0, 0, 0, 255, 255, 255, 255 }) &&
              iterative.Image.Pixels.Cast<byte>().SequenceEqual(otsu.Image.Pixels.Cast<byte>()),
            "Оба метода отделяют светлые пиксели от тёмных, сохраняя размеры");

        var exact = GrayscaleConverter.Convert(MakeRgb([0, 100, 101, 255], 4));
        var binary = GlobalThresholding.Binarize(exact, 100);
        check(binary.Pixels.Cast<byte>().SequenceEqual(new byte[] { 0, 0, 255, 255 }),
            "Яркость, равная порогу, относится к тёмному классу");
        check(exact.Pixels.Cast<byte>().SequenceEqual(new byte[] { 0, 100, 101, 255 }),
            "Пороговая обработка не изменяет исходный массив яркостей");

        foreach (var method in Enum.GetValues<ThresholdMethod>())
        foreach (var value in new byte[] { 0, 128, 255 })
        {
            var result = GlobalThresholding.Apply(MakeRgb([value], 1), method);
            check(result.Threshold == 0 && result.Image[0, 0] == (value == 0 ? 0 : 255),
                $"Однотонное изображение: {method}, яркость {value}, без пустых классов и деления на ноль");
        }

        // Малочисленная яркая группа даёт итеративному методу другой устойчивый порог.
        var uneven = Enumerable.Repeat((byte)10, 100).Concat(Enumerable.Repeat((byte)100, 30))
            .Concat(new byte[] { 200, 200 }).ToArray();
        var unevenSource = MakeRgb(uneven, uneven.Length);
        var unevenOtsu = GlobalThresholding.Apply(unevenSource, ThresholdMethod.Otsu);
        var unevenIterative = GlobalThresholding.Apply(unevenSource, ThresholdMethod.Iterative);
        check(unevenOtsu.Threshold == 10 && unevenIterative.Threshold == 115 &&
              unevenOtsu.Image[100, 0] == 255 && unevenIterative.Image[100, 0] == 0,
            "Методы дают разные разбиения на неравномерном распределении яркостей");

        byte[][] examples =
        [
            [0, 1, 2, 127, 128, 200, 240, 255],
            [10, 10, 11, 50, 90, 90, 190, 191, 255],
            [0, 0, 0, 80, 80, 90, 200, 200, 200],
            uneven
        ];
        foreach (var values in examples)
        {
            var image = GrayscaleConverter.Convert(MakeRgb(values, values.Length));
            var threshold = GlobalThresholding.FindOtsuThreshold(ImageHistogram.Build(image));
            var maximum = Enumerable.Range(0, 255).Max(value => Variance(values, value));
            check(Math.Abs(Variance(values, threshold) - maximum) < 1e-8,
                "Порог Оцу проверен независимым перебором разбиений исходных пикселей");
        }

        var largeHistogram = new long[256];
        largeHistogram[0] = largeHistogram[255] = 3_000_000_000;
        check(GlobalThresholding.FindOtsuThreshold(largeHistogram) == 0 &&
              GlobalThresholding.FindIterativeThreshold(largeHistogram) == 127,
            "Счётчики гистограммы не переполняются при количестве больше int.MaxValue");

        GlobalThresholding.Apply(colors, ThresholdMethod.Otsu);
        check(colors.Pixels.Cast<byte>().SequenceEqual(savedColors),
            "Цветной оригинал сохраняется после обработки");
        using (var bitmap = BitmapPixelAccess.ToBitmap(binary))
        {
            var restored = BitmapPixelAccess.FromBitmap(bitmap);
            check(restored[0, 0] == new RgbPixel(0, 0, 0) &&
                  restored[2, 0] == new RgbPixel(255, 255, 255),
                "Бинарный буфер отображается через прямую запись в Bitmap");
        }

        var negativeHistogram = new long[256];
        negativeHistogram[0] = -1;
        check(Throws<ArgumentException>(() => GlobalThresholding.FindOtsuThreshold(new long[255])) &&
              Throws<ArgumentException>(() => GlobalThresholding.FindIterativeThreshold(new long[256])) &&
              Throws<ArgumentException>(() => GlobalThresholding.FindOtsuThreshold(negativeHistogram)),
            "Некорректная и пустая гистограммы отклоняются");
        check(Throws<ArgumentOutOfRangeException>(() => GlobalThresholding.Binarize(gray, -1)) &&
              Throws<ArgumentOutOfRangeException>(() => GlobalThresholding.Binarize(gray, 256)) &&
              Throws<ArgumentOutOfRangeException>(() => GlobalThresholding.Apply(colors, (ThresholdMethod)10)),
            "Порог и метод проверяются до обработки");
        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        check(Throws<OperationCanceledException>(() => GlobalThresholding.Apply(colors,
                ThresholdMethod.Otsu, canceled.Token)), "Отменённая обработка завершается сразу");
    }

    private static PixelBuffer MakeRgb(byte[] values, int width)
    {
        var image = new PixelBuffer(width, values.Length / width);
        for (var y = 0; y < image.Height; y++)
        for (var x = 0; x < width; x++)
        {
            var value = values[y * width + x];
            image[x, y] = new RgbPixel(value, value, value);
        }
        return image;
    }

    private static double Variance(byte[] values, int threshold)
    {
        var dark = values.Where(value => value <= threshold).ToArray();
        var light = values.Where(value => value > threshold).ToArray();
        if (dark.Length == 0 || light.Length == 0)
            return 0;
        var difference = dark.Average(value => (double)value) - light.Average(value => (double)value);
        return (double)dark.Length / values.Length * light.Length / values.Length * difference * difference;
    }

    private static bool Throws<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return true; }
        return false;
    }
}
