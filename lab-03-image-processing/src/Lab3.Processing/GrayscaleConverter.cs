using Lab3.PixelAccess;

namespace Lab3.Processing;

public static class GrayscaleConverter
{
    public static GrayBuffer Convert(PixelBuffer source, CancellationToken cancellationToken = default,
        bool parallel = true)
    {
        ArgumentNullException.ThrowIfNull(source);
        var gray = new GrayBuffer(source.Width, source.Height);
        RowProcessing.For(source.Width, source.Height, parallel, cancellationToken, y =>
        {
            for (var x = 0; x < source.Width; x++)
            {
                var r = source.Pixels[y, x, 0];
                var g = source.Pixels[y, x, 1];
                var b = source.Pixels[y, x, 2];
                // Y = 0.299R + 0.587G + 0.114B, с округлением до целого.
                gray.Pixels[y, x] = (byte)((299 * r + 587 * g + 114 * b + 500) / 1000);
            }
        });
        return gray;
    }
}
