using System.Runtime.InteropServices;
using Lab3.PixelAccess;

namespace Lab3.Presentation;

public partial class MainForm : Form
{
    private PixelBuffer? _sourceBuffer;

    public MainForm()
    {
        InitializeComponent();
    }

    private async void OpenButton_Click(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Открыть изображение",
            Filter = "Изображения|*.bmp;*.png;*.jpg;*.jpeg;*.gif;*.tif;*.tiff|Все файлы|*.*",
            RestoreDirectory = true
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        var filePath = dialog.FileName;
        openButton.Enabled = false;
        loadProgressBar.Visible = true;
        statusLabel.Text = "Загрузка изображения...";

        try
        {
            var loaded = await Task.Run(() =>
            {
                var buffer = BitmapPixelAccess.Load(filePath);
                return (Buffer: buffer, Preview: BitmapPixelAccess.ToBitmap(buffer));
            });

            if (IsDisposed || Disposing)
            {
                loaded.Preview.Dispose();
                return;
            }

            _sourceBuffer = loaded.Buffer;
            SetImage(sourcePictureBox, loaded.Preview);
            SetImage(resultPictureBox, null);
            resultPlaceholder.Visible = true;
            statusLabel.Text = $"{Path.GetFileName(filePath)} — " +
                $"{_sourceBuffer.Width} × {_sourceBuffer.Height} пикселей";
        }
        catch (Exception error) when (error is ArgumentException or IOException or
                                      UnauthorizedAccessException or ExternalException or OutOfMemoryException)
        {
            if (!IsDisposed && !Disposing)
            {
                statusLabel.Text = "Не удалось открыть изображение.";
                MessageBox.Show(this, "Не удалось прочитать изображение. Файл может быть повреждён " +
                    "или иметь неподдерживаемый формат.\n\n" + error.Message,
                    "Ошибка загрузки", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        finally
        {
            if (!IsDisposed && !Disposing)
            {
                openButton.Enabled = true;
                loadProgressBar.Visible = false;
            }
        }
    }

    private static void SetImage(PictureBox pictureBox, Bitmap? image)
    {
        var previous = pictureBox.Image;
        pictureBox.Image = image;
        previous?.Dispose();
    }

}
