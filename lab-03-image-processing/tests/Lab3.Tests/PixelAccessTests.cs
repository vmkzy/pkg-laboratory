using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Lab3.PixelAccess;

internal static class PixelAccessTests
{
    public static void Run(Action<bool, string> check)
    {
        var buffer = new PixelBuffer(3, 2);
        buffer[0, 0] = new RgbPixel(255, 0, 0);
        buffer[1, 0] = new RgbPixel(0, 255, 0);
        buffer[2, 0] = new RgbPixel(0, 0, 255);
        buffer[0, 1] = new RgbPixel(10, 20, 30);
        buffer[1, 1] = new RgbPixel(40, 50, 60);
        buffer[2, 1] = new RgbPixel(70, 80, 90);

        check(buffer.Pixels[1, 2, 0] == 70 && buffer.Pixels[1, 2, 2] == 90,
            "RGB хранится в порядке строка, столбец, канал");
        check(buffer.Sample(-3, -2, EdgeMode.Clamp) == buffer[0, 0] &&
              buffer.Sample(8, 7, EdgeMode.Clamp) == buffer[2, 1],
            "Clamp дублирует ближайший граничный пиксель");
        check(buffer.Sample(-2, -2, EdgeMode.Reflect) == buffer[1, 1] &&
              buffer.Sample(4, 2, EdgeMode.Reflect) == buffer[1, 1],
            "Reflect отражает обе координаты");
        check(buffer.Sample(1, 1, EdgeMode.Clamp) == buffer[1, 1] &&
              buffer.Sample(1, 1, EdgeMode.Reflect) == buffer[1, 1],
            "Пиксель внутри изображения не меняется");

        int[] coordinates = [-9, -8, -5, -4, -2, -1, 0, 3, 4, 5, 7, 8, 9];
        int[] reflected = [0, 0, 3, 3, 1, 0, 0, 3, 3, 2, 0, 0, 1];
        check(coordinates.Select(value => EdgePadding.Map(value, 4, EdgeMode.Reflect))
                .SequenceEqual(reflected), "Reflect работает за несколько периодов от края");
        check(EdgePadding.Map(int.MinValue, 1, EdgeMode.Reflect) == 0 &&
              EdgePadding.Map(int.MaxValue, 1, EdgeMode.Clamp) == 0,
            "Изображение шириной в один пиксель не вызывает деление на ноль");
        check(EdgePadding.Map(int.MaxValue, int.MaxValue, EdgeMode.Reflect) == int.MaxValue - 1,
            "Период Reflect вычисляется без переполнения int");
        check(Throws<ArgumentOutOfRangeException>(() => new PixelBuffer(0, 1)) &&
              Throws<ArgumentOutOfRangeException>(() => new PixelBuffer(1, -1)),
            "Размеры буфера должны быть положительными");
        check(Throws<ArgumentOutOfRangeException>(() => EdgePadding.Map(0, 0, EdgeMode.Clamp)) &&
              Throws<ArgumentOutOfRangeException>(() => EdgePadding.Map(0, 1, (EdgeMode)10)),
            "Некорректные параметры границ обнаруживаются");

        using var bitmap = BitmapPixelAccess.ToBitmap(buffer);
        var restored = BitmapPixelAccess.FromBitmap(bitmap);
        check(SamePixels(buffer, restored), "Буфер проходит запись и чтение Bitmap без потери RGB");
        var raw = ReadFirstRow(bitmap);
        check(raw.Take(12).SequenceEqual(new byte[]
            { 0, 0, 255, 255, 0, 255, 0, 255, 255, 0, 0, 255 }),
            "В Bitmap записываются BGRA и непрозрачный альфа-канал");

        using var padded = MakeBitmap(3, 2, PixelFormat.Format24bppRgb,
            [30, 20, 10, 60, 50, 40, 90, 80, 70, 0, 0, 0],
            [120, 110, 100, 150, 140, 130, 180, 170, 160, 0, 0, 0]);
        var paddedBuffer = BitmapPixelAccess.FromBitmap(padded);
        check(paddedBuffer[0, 0] == new RgbPixel(10, 20, 30) &&
              paddedBuffer[2, 1] == new RgbPixel(160, 170, 180),
            "24-битное изображение с выравниванием строк читается без сдвига");

        using var transparent = MakeBitmap(2, 1, PixelFormat.Format32bppArgb,
            [30, 20, 10, 0, 0, 0, 255, 128]);
        var transparentBuffer = BitmapPixelAccess.FromBitmap(transparent);
        check(transparentBuffer[0, 0] == new RgbPixel(255, 255, 255) &&
              transparentBuffer[1, 0] == new RgbPixel(255, 127, 127),
            "Прозрачность накладывается на белый фон вручную");

        using var indexed = MakeBitmap(3, 1, PixelFormat.Format8bppIndexed, [0, 1, 2, 0]);
        var palette = indexed.Palette;
        palette.Entries[0] = Color.Red;
        palette.Entries[1] = Color.Lime;
        palette.Entries[2] = Color.Blue;
        indexed.Palette = palette;
        var indexedBuffer = BitmapPixelAccess.FromBitmap(indexed);
        check(indexedBuffer[0, 0] == new RgbPixel(255, 0, 0) &&
              indexedBuffer[1, 0] == new RgbPixel(0, 255, 0) &&
              indexedBuffer[2, 0] == new RgbPixel(0, 0, 255),
            "Индексированное изображение читается с учётом палитры");

        CheckNegativeStride(check);
        CheckFileLoading(buffer, bitmap, check);
    }

    private static void CheckNegativeStride(Action<bool, string> check)
    {
        var memory = Marshal.AllocHGlobal(16);
        try
        {
            // Сначала физически нижняя строка, затем верхняя; Scan0 указывает на верхнюю.
            Marshal.Copy(new byte[] { 255, 0, 0, 255, 255, 255, 255, 255,
                                      0, 0, 255, 255, 0, 255, 0, 255 }, 0, memory, 16);
            using var bitmap = new Bitmap(2, 2, -8, PixelFormat.Format32bppArgb, IntPtr.Add(memory, 8));
            var buffer = BitmapPixelAccess.FromBitmap(bitmap);
            check(buffer[0, 0] == new RgbPixel(255, 0, 0) &&
                  buffer[1, 0] == new RgbPixel(0, 255, 0) &&
                  buffer[0, 1] == new RgbPixel(0, 0, 255) &&
                  buffer[1, 1] == new RgbPixel(255, 255, 255),
                "Отрицательный stride не переворачивает изображение");
        }
        finally
        {
            Marshal.FreeHGlobal(memory);
        }
    }

    private static void CheckFileLoading(PixelBuffer expected, Bitmap bitmap, Action<bool, string> check)
    {
        var path = Path.Combine(Path.GetTempPath(), $"lab3-pixels-{Guid.NewGuid():N}.png");
        try
        {
            bitmap.Save(path, ImageFormat.Png);
            var actual = BitmapPixelAccess.Load(path);
            check(SamePixels(expected, actual), "PNG с диска загружается без потери цвета");
            using (File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                check(true, "После загрузки файл доступен для изменения");

            File.WriteAllText(path, "not an image");
            check(Throws<ArgumentException>(() => BitmapPixelAccess.Load(path)),
                "Повреждённый файл не принимается за изображение");
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static Bitmap MakeBitmap(int width, int height, PixelFormat format, params byte[][] rows)
    {
        var bitmap = new Bitmap(width, height, format);
        var data = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, format);
        try
        {
            for (var y = 0; y < height; y++)
                Marshal.Copy(rows[y], 0, IntPtr.Add(data.Scan0, y * data.Stride), rows[y].Length);
        }
        finally
        {
            bitmap.UnlockBits(data);
        }
        return bitmap;
    }

    private static byte[] ReadFirstRow(Bitmap bitmap)
    {
        var data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
            ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        try
        {
            var row = new byte[bitmap.Width * 4];
            Marshal.Copy(data.Scan0, row, 0, row.Length);
            return row;
        }
        finally
        {
            bitmap.UnlockBits(data);
        }
    }

    private static bool SamePixels(PixelBuffer first, PixelBuffer second) =>
        first.Width == second.Width && first.Height == second.Height &&
        first.Pixels.Cast<byte>().SequenceEqual(second.Pixels.Cast<byte>());

    private static bool Throws<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return true; }
        return false;
    }
}
