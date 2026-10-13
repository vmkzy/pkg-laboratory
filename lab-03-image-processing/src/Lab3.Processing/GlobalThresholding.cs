using Lab3.PixelAccess;

namespace Lab3.Processing;

public static class GlobalThresholding
{
    public static ThresholdResult Apply(PixelBuffer source, ThresholdMethod method,
        CancellationToken cancellationToken = default)
    {
        if (method is not (ThresholdMethod.Otsu or ThresholdMethod.Iterative))
            throw new ArgumentOutOfRangeException(nameof(method));

        var gray = GrayscaleConverter.Convert(source, cancellationToken);
        var histogram = ImageHistogram.Build(gray, cancellationToken);
        var threshold = method == ThresholdMethod.Otsu
            ? FindOtsuThreshold(histogram)
            : FindIterativeThreshold(histogram);
        return new ThresholdResult(Binarize(gray, threshold, cancellationToken), threshold);
    }

    public static int FindOtsuThreshold(long[] histogram)
    {
        var (totalCount, totalSum) = GetTotals(histogram);
        long darkCount = 0;
        double darkSum = 0;
        double bestVariance = -1;
        var bestThreshold = 0;

        for (var threshold = 0; threshold < 255; threshold++)
        {
            darkCount += histogram[threshold];
            darkSum += threshold * (double)histogram[threshold];
            if (darkCount == 0)
                continue;

            var lightCount = totalCount - darkCount;
            if (lightCount == 0)
                break;

            var darkMean = darkSum / darkCount;
            var lightMean = (totalSum - darkSum) / lightCount;
            var darkWeight = (double)darkCount / totalCount;
            var lightWeight = (double)lightCount / totalCount;
            var difference = darkMean - lightMean;
            var variance = darkWeight * lightWeight * difference * difference;

            // При одинаковой дисперсии оставляем первый найденный порог.
            if (variance > bestVariance)
            {
                bestVariance = variance;
                bestThreshold = threshold;
            }
        }

        // У однотонной картинки двух классов нет: остаётся порог 0.
        return bestThreshold;
    }

    public static int FindIterativeThreshold(long[] histogram)
    {
        var (totalCount, totalSum) = GetTotals(histogram);
        var minimum = 0;
        var maximum = 255;
        while (histogram[minimum] == 0) minimum++;
        while (histogram[maximum] == 0) maximum--;
        if (minimum == maximum)
            return 0;

        var threshold = (minimum + maximum) / 2;
        for (var iteration = 0; iteration < 256; iteration++)
        {
            long darkCount = 0;
            double darkSum = 0;
            for (var value = 0; value <= threshold; value++)
            {
                darkCount += histogram[value];
                darkSum += value * (double)histogram[value];
            }

            var lightCount = totalCount - darkCount;
            if (darkCount == 0 || lightCount == 0)
                return threshold;

            var darkMean = darkSum / darkCount;
            var lightMean = (totalSum - darkSum) / lightCount;
            // Новый порог — середина между средними яркостями двух классов.
            var nextThreshold = (int)((darkMean + lightMean) / 2);
            if (nextThreshold == threshold)
                return threshold;
            threshold = nextThreshold;
        }
        return threshold;
    }

    public static GrayBuffer Binarize(GrayBuffer source, int threshold,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentOutOfRangeException.ThrowIfNegative(threshold);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(threshold, 255);
        var result = new GrayBuffer(source.Width, source.Height);
        for (var y = 0; y < source.Height; y++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            for (var x = 0; x < source.Width; x++)
                result.Pixels[y, x] = source.Pixels[y, x] > threshold ? (byte)255 : (byte)0;
        }
        return result;
    }

    private static (long Count, double Sum) GetTotals(long[] histogram)
    {
        ArgumentNullException.ThrowIfNull(histogram);
        if (histogram.Length != 256)
            throw new ArgumentException("Гистограмма должна содержать 256 значений.", nameof(histogram));

        long count = 0;
        double sum = 0;
        for (var value = 0; value < histogram.Length; value++)
        {
            if (histogram[value] < 0)
                throw new ArgumentException("Число пикселей не может быть отрицательным.", nameof(histogram));
            count = checked(count + histogram[value]);
            sum += value * (double)histogram[value];
        }
        if (count == 0)
            throw new ArgumentException("Гистограмма не содержит пикселей.", nameof(histogram));
        return (count, sum);
    }
}
