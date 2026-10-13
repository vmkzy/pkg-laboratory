using Lab3.PixelAccess;

namespace Lab3.Processing;

public static class ImageHistogram
{
    public static long[] Build(GrayBuffer image, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(image);
        var histogram = new long[256];
        for (var y = 0; y < image.Height; y++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            for (var x = 0; x < image.Width; x++)
                histogram[image.Pixels[y, x]]++;
        }
        return histogram;
    }
}
