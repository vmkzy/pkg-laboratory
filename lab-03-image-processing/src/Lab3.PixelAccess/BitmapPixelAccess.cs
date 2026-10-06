using System.Drawing.Imaging;

namespace Lab3.PixelAccess;

public static class BitmapPixelAccess
{
    public static PixelBuffer Load(string filePath)
    {
        using var bitmap = new Bitmap(filePath);
        return FromBitmap(bitmap);
    }

    public static unsafe PixelBuffer FromBitmap(Bitmap bitmap)
    {
        ArgumentNullException.ThrowIfNull(bitmap);
        var buffer = new PixelBuffer(bitmap.Width, bitmap.Height);
        var rectangle = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
        var data = bitmap.LockBits(rectangle, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

        try
        {
            for (var y = 0; y < buffer.Height; y++)
            {
                var row = (byte*)data.Scan0 + (long)y * data.Stride;
                for (var x = 0; x < buffer.Width; x++)
                {
                    var pixel = row + x * 4;
                    var alpha = pixel[3];
                    // В памяти Bitmap каналы идут B, G, R, A. Прозрачность накладываем на белый фон.
                    buffer.Pixels[y, x, 0] = CompositeOnWhite(pixel[2], alpha);
                    buffer.Pixels[y, x, 1] = CompositeOnWhite(pixel[1], alpha);
                    buffer.Pixels[y, x, 2] = CompositeOnWhite(pixel[0], alpha);
                }
            }
        }
        finally
        {
            bitmap.UnlockBits(data);
        }

        return buffer;
    }

    public static unsafe Bitmap ToBitmap(PixelBuffer buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        var bitmap = new Bitmap(buffer.Width, buffer.Height, PixelFormat.Format32bppArgb);

        try
        {
            var rectangle = new Rectangle(0, 0, buffer.Width, buffer.Height);
            var data = bitmap.LockBits(rectangle, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            try
            {
                for (var y = 0; y < buffer.Height; y++)
                {
                    var row = (byte*)data.Scan0 + (long)y * data.Stride;
                    for (var x = 0; x < buffer.Width; x++)
                    {
                        var pixel = row + x * 4;
                        pixel[0] = buffer.Pixels[y, x, 2];
                        pixel[1] = buffer.Pixels[y, x, 1];
                        pixel[2] = buffer.Pixels[y, x, 0];
                        pixel[3] = 255;
                    }
                }
            }
            finally
            {
                bitmap.UnlockBits(data);
            }

            return bitmap;
        }
        catch
        {
            bitmap.Dispose();
            throw;
        }
    }

    private static byte CompositeOnWhite(byte channel, byte alpha) =>
        (byte)((channel * alpha + 255 * (255 - alpha) + 127) / 255);
}
