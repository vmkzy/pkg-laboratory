using System.Drawing.Imaging;
using System.Text.Json;
using Lab3.PixelAccess;
using Lab3.Processing;

internal static class SampleImages
{
    public static void Generate(string directory)
    {
        Directory.CreateDirectory(directory);
        Directory.CreateDirectory(Path.Combine(directory, "reference"));
        var regions = Create(320, 200, (x, y) => x < 160 ? (byte)40 : (byte)210);
        Save(regions, Path.Combine(directory, "01-two-regions.png"));
        var mask = TextMask();
        Save(mask, Path.Combine(directory, "reference", "text-mask.png"));
        var text = Create(mask.Width, mask.Height, (x, y) =>
        {
            var background = 65 + 175.0 * x / (mask.Width - 1) + 5 * Math.Sin(y / 25.0);
            return (byte)Math.Clamp(Math.Round(mask[x, y] == 0 ? background * 0.4 : background), 0, 255);
        });
        Save(text, Path.Combine(directory, "02-uneven-text.png"));
        var shapes = Create(320, 200, (x, y) => IsShape(x, y) ? (byte)210 : (byte)40);
        Save(shapes, Path.Combine(directory, "03-shapes.png"));
        var random = new Random(9);
        var noisy = Create(320, 200, (x, y) =>
        {
            var noise = Math.Sqrt(-2 * Math.Log(1 - random.NextDouble())) * Math.Cos(2 * Math.PI * random.NextDouble()) * 22;
            return (byte)Math.Clamp(Math.Round(shapes[x, y] + noise), 0, 255);
        });
        Save(noisy, Path.Combine(directory, "04-noisy-shapes.png"));
        var blurred = SpatialFilters.Gaussian(shapes, 2.5, EdgeMode.Reflect);
        Save(Create(320, 200, (x, y) => (byte)Math.Round(blurred.Pixels[y, x])), Path.Combine(directory, "05-blurred-shapes.png"));
        Save(Create(320, 200, (x, y) => IsShape(x, y) ? (byte)145 : (byte)110), Path.Combine(directory, "06-low-contrast.png"));
        var lines = Create(320, 200, (x, y) =>
            (y == 35 && x is > 20 and < 140) || (x == 175 && y is > 20 and < 130) ||
            (x - y == 190 && y is > 20 and < 100) || (x == 45 && y == 150) || (x == 100 && y == 170)
                ? (byte)240 : (byte)20);
        Save(lines, Path.Combine(directory, "07-points-lines.png"));
    }

    public static void Analyze(string samples, string directory)
    {
        Directory.CreateDirectory(directory);
        var measurements = new List<object>();
        var mask = BitmapPixelAccess.Load(Path.Combine(samples, "reference", "text-mask.png"));
        foreach (var name in new[] { "01-two-regions", "02-uneven-text", "04-noisy-shapes", "07-points-lines" })
        {
            var source = BitmapPixelAccess.Load(Path.Combine(samples, name + ".png"));
            var methods = name == "04-noisy-shapes" ? new[] { ProcessingMethod.Sobel, ProcessingMethod.Canny }
                : name == "07-points-lines" ? new[] { ProcessingMethod.Points, ProcessingMethod.Lines }
                : new[] { ProcessingMethod.Otsu, ProcessingMethod.Iterative, ProcessingMethod.Sauvola };
            foreach (var method in methods)
            {
                var settings = new ProcessingSettings { Method = method, HighThreshold = name == "07-points-lines" ? 900 : 80 };
                var result = ImageProcessor.Process(source, settings);
                Save(result.Image, Path.Combine(directory, name + "-" + method + ".png"));
                if (result.Directions is not null)
                {
                    using var bitmap = BitmapPixelAccess.ToBitmap(result.Directions);
                    bitmap.Save(Path.Combine(directory, name + "-" + method + "-directions.png"), ImageFormat.Png);
                }
                long tp = 0, fp = 0, fn = 0;
                if (name == "02-uneven-text")
                    for (var y = 0; y < mask.Height; y++)
                        for (var x = 0; x < mask.Width; x++)
                        {
                            var expected = mask.Pixels[y, x, 0] == 0;
                            var actual = result.Image[x, y] == 0;
                            if (expected && actual) tp++;
                            if (!expected && actual) fp++;
                            if (expected && !actual) fn++;
                        }
                measurements.Add(new { Sample = name, Method = method.ToString(), result.Threshold,
                    White = result.AfterHistogram[255], Black = result.AfterHistogram[0],
                    F1 = 2 * tp + fp + fn == 0 ? (double?)null : 2.0 * tp / (2 * tp + fp + fn),
                    Before = result.BeforeHistogram, After = result.AfterHistogram,
                    EdgePoints = result.EdgePoints.Count });
            }
        }
        File.WriteAllText(Path.Combine(directory, "measurements.json"), JsonSerializer.Serialize(measurements, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static bool IsShape(int x, int y) => (x is >= 35 and <= 130 && y is >= 35 and <= 155)
        || (x - 230) * (x - 230) + (y - 95) * (y - 95) <= 55 * 55;

    private static GrayBuffer TextMask()
    {
        var mask = Create(320, 200, (_, _) => 255);
        string[] glyphs = ["10000100001000010000100001000011111", "01110100011000111111100011000110001", "11110100011000111110100011000111110", "11110000010000101110000010000111110"];
        for (var row = 0; row < 4; row++)
            for (var letter = 0; letter < 12; letter++)
            {
                var glyph = glyphs[letter % 4];
                for (var y = 0; y < 7; y++)
                    for (var x = 0; x < 5; x++)
                        if (glyph[y * 5 + x] == '1')
                            for (var dy = 0; dy < 3; dy++)
                                for (var dx = 0; dx < 3; dx++) mask[15 + letter * 24 + x * 3 + dx, 25 + row * 42 + y * 3 + dy] = 0;
            }
        return mask;
    }

    private static GrayBuffer Create(int width, int height, Func<int, int, byte> value)
    {
        var result = new GrayBuffer(width, height);
        for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++) result[x, y] = value(x, y);
        return result;
    }

    private static void Save(GrayBuffer buffer, string path)
    {
        using var bitmap = BitmapPixelAccess.ToBitmap(buffer);
        bitmap.Save(path, ImageFormat.Png);
    }
}
