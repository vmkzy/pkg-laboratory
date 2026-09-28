using Lab2.Core.Models;
using Lab2.Core.Services;
using Lab2.Infrastructure.Parsers;
using System.Runtime.InteropServices;
using ImageLockMode = System.Drawing.Imaging.ImageLockMode;
using PixelFormat = System.Drawing.Imaging.PixelFormat;

namespace Lab2.Presentation;

public partial class MainForm : Form
{
    private readonly FolderScanner _scanner = new([
        new BmpParser(), new PngParser(), new JpegParser(), new GifParser(),
        new TiffParser(), new PcxParser()
    ]);
    private readonly List<ImageMetadata> _results = [];
    private CancellationTokenSource? _scanCancellation;
    private int _previewVersion;
    private int _displayedRow = -1;

    public MainForm()
    {
        InitializeComponent();
        FormClosing += (_, _) => _scanCancellation?.Cancel();
    }

    private void BrowseButton_Click(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Выберите папку с графическими файлами",
            ShowNewFolderButton = false,
            UseDescriptionForTitle = true
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        folderPathTextBox.Text = dialog.SelectedPath;
        startButton.Enabled = true;
        statusLabel.Text = "Папка выбрана. Можно начать анализ.";
    }

    private async void StartButton_Click(object? sender, EventArgs e)
    {
        var folder = folderPathTextBox.Text;
        if (!Directory.Exists(folder))
        {
            statusLabel.Text = "Выбранная папка больше не существует.";
            return;
        }

        _scanCancellation = new CancellationTokenSource();
        var cancellation = _scanCancellation;
        _results.Clear();
        _displayedRow = -1;
        _previewVersion++;
        resultsGrid.RowCount = 0;
        SetPreview(null);
        detailsValueLabel.Text = "Выберите файл в таблице";
        browseButton.Enabled = false;
        startButton.Enabled = false;
        cancelButton.Enabled = true;
        subdirectoriesCheckBox.Enabled = false;
        scanProgressBar.Style = ProgressBarStyle.Marquee;
        countersLabel.Text = "Поиск файлов...";
        statusLabel.Text = "Сканирование начато";

        var progress = new Progress<ScanProgress>(value =>
        {
            if (!IsDisposed && ReferenceEquals(_scanCancellation, cancellation))
                UpdateProgress(value);
        });

        try
        {
            var request = new ScanRequest
            {
                FolderPath = folder,
                IncludeSubdirectories = subdirectoriesCheckBox.Checked,
                WorkerCount = Math.Clamp(Environment.ProcessorCount, 1, 16)
            };
            await foreach (var item in _scanner.ScanAsync(request, progress, cancellation.Token))
            {
                if (IsDisposed) break;
                _results.Add(item);
                // Virtual grid stores only metadata; repaint in batches, not 100 000 times.
                if (_results.Count == 1 || _results.Count % 128 == 0)
                    resultsGrid.RowCount = _results.Count;
            }

            if (!IsDisposed)
            {
                resultsGrid.RowCount = _results.Count;
                scanProgressBar.Style = ProgressBarStyle.Blocks;
                scanProgressBar.Value = 100;
                var problemCount = _results.Count(item => item.Status != FileProcessingStatus.Valid);
                countersLabel.Text = $"Обработано: {_results.Count:N0}   Проблем: {problemCount:N0}";
                statusLabel.Text = $"Готово. Найдено файлов: {_results.Count:N0}.";
            }
        }
        catch (OperationCanceledException)
        {
            if (!IsDisposed)
                statusLabel.Text = $"Сканирование отменено. Обработано: {_results.Count:N0}.";
        }
        catch (Exception error)
        {
            if (!IsDisposed)
                statusLabel.Text = $"Не удалось завершить сканирование: {error.Message}";
        }
        finally
        {
            cancellation.Dispose();
            if (!IsDisposed)
            {
                resultsGrid.RowCount = _results.Count;
                scanProgressBar.Style = ProgressBarStyle.Blocks;
                if (ReferenceEquals(_scanCancellation, cancellation))
                    _scanCancellation = null;
                browseButton.Enabled = true;
                startButton.Enabled = Directory.Exists(folderPathTextBox.Text);
                cancelButton.Enabled = false;
                subdirectoriesCheckBox.Enabled = true;
            }
        }
    }

    private void CancelButton_Click(object? sender, EventArgs e)
    {
        cancelButton.Enabled = false;
        _scanCancellation?.Cancel();
    }

    private void UpdateProgress(ScanProgress progress)
    {
        if (progress.EnumerationCompleted)
        {
            scanProgressBar.Style = ProgressBarStyle.Blocks;
            scanProgressBar.Value = progress.Percentage;
            statusLabel.Text = "Обработка файлов...";
        }
        else
        {
            scanProgressBar.Style = ProgressBarStyle.Marquee;
            statusLabel.Text = "Поиск файлов во вложенных папках...";
        }

        countersLabel.Text = $"Найдено: {progress.DiscoveredFiles:N0}   " +
                             $"Обработано: {progress.ProcessedFiles:N0}   " +
                             $"Проблем: {progress.ProblemFiles:N0}";
    }

    private void ResultsGrid_CellValueNeeded(object? sender, DataGridViewCellValueEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= _results.Count) return;
        var item = _results[e.RowIndex];
        e.Value = e.ColumnIndex switch
        {
            0 => item.FileName,
            1 => item.Format == ImageFormat.Unknown ? "—" : item.Format.ToString().ToUpperInvariant(),
            2 => item.Width is > 0 && item.Height is > 0 ? $"{item.Width} × {item.Height}" : "—",
            3 => FormatDpi(item),
            4 => item.ColorDepth is { } depth ? $"{depth} бит" : "—",
            5 => item.Compression,
            6 => StatusName(item.Status),
            _ => ""
        };
    }

    private async void ResultsGrid_SelectionChanged(object? sender, EventArgs e)
    {
        var index = resultsGrid.CurrentRow?.Index ?? -1;
        if (index == _displayedRow || index < 0 || index >= _results.Count)
            return;

        _displayedRow = index;
        var version = ++_previewVersion;
        var item = _results[index];
        SetPreview(null);
        detailsValueLabel.Text = $"{item.FilePath}\n" +
            $"Размер файла: {item.FileSizeBytes:N0} байт\n" +
            $"Формат: {item.Format}\n" +
            $"Размер: {item.Width?.ToString() ?? "—"} × {item.Height?.ToString() ?? "—"}\n" +
            $"DPI: {FormatDpi(item)}\n" +
            $"Глубина: {item.ColorDepth?.ToString() ?? "—"} бит\n" +
            $"Сжатие: {item.Compression}\n" +
            $"Статус: {StatusName(item.Status)}\n" +
            (item.ErrorMessage is null ? "" : $"Причина: {item.ErrorMessage}");

        if (item.Status != FileProcessingStatus.Valid) return;

        try
        {
            // Rendering only: metadata above always comes from our own byte parser.
            var bitmap = await Task.Run(() => CreatePreview(item.FilePath, item.Format));
            if (IsDisposed || version != _previewVersion)
                bitmap.Dispose();
            else
                SetPreview(bitmap);
        }
        catch (Exception error)
        {
            if (!IsDisposed && version == _previewVersion)
                detailsValueLabel.Text += $"\nПредпросмотр недоступен: {error.Message}";
        }
    }

    private static Bitmap CreatePreview(string path, ImageFormat format)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete);
        if (format == ImageFormat.Pcx)
        {
            var preview = PcxPreviewDecoder.Decode(stream);
            var bitmap = new Bitmap(preview.Width, preview.Height, PixelFormat.Format24bppRgb);
            var area = new Rectangle(0, 0, preview.Width, preview.Height);
            var data = bitmap.LockBits(area, ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
            try
            {
                for (var row = 0; row < preview.Height; row++)
                    Marshal.Copy(preview.BgrPixels, row * preview.Width * 3,
                        IntPtr.Add(data.Scan0, row * data.Stride), preview.Width * 3);
            }
            finally
            {
                bitmap.UnlockBits(data);
            }
            return bitmap;
        }

        using var source = Image.FromStream(stream, false, true);
        using var thumbnail = source.GetThumbnailImage(320, 240, () => false, IntPtr.Zero);
        return new Bitmap(thumbnail);
    }

    private void SetPreview(Image? image)
    {
        var previous = previewPictureBox.Image;
        previewPictureBox.Image = image;
        previous?.Dispose();
    }

    private static string FormatDpi(ImageMetadata item) =>
        item.DpiX is { } x && item.DpiY is { } y ? $"{x:0.#} × {y:0.#}" : "—";

    private static string StatusName(FileProcessingStatus status) => status switch
    {
        FileProcessingStatus.Valid => "Корректен",
        FileProcessingStatus.Corrupted => "Повреждён",
        FileProcessingStatus.Unsupported => "Не поддерживается",
        FileProcessingStatus.Failed => "Ошибка чтения",
        _ => "Неизвестно"
    };

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        ++_previewVersion;
        SetPreview(null);
        base.OnFormClosed(e);
    }
}
