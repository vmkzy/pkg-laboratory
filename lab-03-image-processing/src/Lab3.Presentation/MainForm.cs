using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using Lab3.PixelAccess;
using Lab3.Processing;

namespace Lab3.Presentation;

public partial class MainForm : Form
{
    private PixelBuffer? _sourceBuffer;
    private ProcessingResult? _result;
    private Bitmap? _binaryPreview;
    private Bitmap? _directionPreview;
    private CancellationTokenSource? _cancellation;
    private bool _busy;
    private readonly FlowLayoutPanel _parameters = new() { AutoSize = true, Dock = DockStyle.Top, Padding = new(8, 0, 8, 4) };
    private readonly TrackBar _radius = new() { Minimum = 1, Maximum = 50, Value = 7, Width = 130, Height = 35, TickFrequency = 7 };
    private readonly Label _windowLabel = new() { AutoSize = true, Text = "Окно 15 × 15" };
    private readonly NumericUpDown _k = new() { DecimalPlaces = 2, Increment = 0.05m, Minimum = 0, Maximum = 1, Value = 0.2m, Width = 65 };
    private readonly NumericUpDown _sigma = new() { DecimalPlaces = 1, Increment = 0.1m, Minimum = 0.3m, Maximum = 10, Value = 1.2m, Width = 65 };
    private readonly NumericUpDown _low = new() { Minimum = 1, Maximum = 2040, Value = 30, Width = 65 };
    private readonly NumericUpDown _high = new() { Minimum = 1, Maximum = 2040, Value = 80, Width = 65 };
    private readonly ComboBox _border = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 145 };
    private readonly ComboBox _line = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 135 };
    private readonly CheckBox _parallel = new() { AutoSize = true, Text = "Параллельно", Checked = true };
    private readonly CheckBox _directions = new() { AutoSize = true, Text = "Направления границ" };
    private readonly Button _cancel = new() { AutoSize = true, Text = "Отмена", Enabled = false };
    private readonly Button _save = new() { AutoSize = true, Text = "Сохранить PNG", Enabled = false };
    private readonly Button _csv = new() { AutoSize = true, Text = "Координаты CSV", Enabled = false };
    private readonly HistogramControl _beforeHistogram = new() { Dock = DockStyle.Fill, Caption = "Гистограмма исходной яркости" };
    private readonly HistogramControl _afterHistogram = new() { Dock = DockStyle.Fill, Caption = "Гистограмма бинарного результата" };

    public MainForm()
    {
        InitializeComponent();
        InitializeProcessingControls();
        InitializeResponsiveLayout();
        FormClosing += (_, _) => _cancellation?.Cancel();
    }

    private void InitializeProcessingControls()
    {
        methodComboBox.Items.Clear();
        methodComboBox.Items.AddRange(new object[] { "Метод Оцу", "Итеративный метод", "Саувола", "Обнаружение точек", "Обнаружение линий", "Собель", "Кэнни" });
        methodComboBox.SelectedIndex = 0;
        openButton.Padding = new Padding(4, 2, 4, 2);
        processButton.Padding = new Padding(4, 2, 4, 2);
        methodComboBox.SelectedIndexChanged += (_, _) => UpdateParameters();
        _border.Items.AddRange(new object[] { "Clamp — повтор", "Reflect — отражение" });
        _border.SelectedIndex = 1;
        _line.Items.AddRange(new object[] { "Все направления", "Горизонтальные", "Вертикальные", "Диагональ ↘", "Диагональ ↗" });
        _line.SelectedIndex = 0;
        _directions.Enabled = false;
        _radius.ValueChanged += (_, _) => _windowLabel.Text = $"Окно {2 * _radius.Value + 1} × {2 * _radius.Value + 1}";
        _directions.CheckedChanged += (_, _) => ShowResult();
        _cancel.Click += (_, _) => _cancellation?.Cancel();
        _save.Click += SaveButton_Click;
        _csv.Click += CsvButton_Click;
        toolbar.Controls.AddRange(new Control[] { _cancel, _save, _csv });
        AddParameter("Границы:", _border);
        AddParameter("Радиус:", _radius, _windowLabel);
        AddParameter("k:", _k);
        AddParameter("σ:", _sigma);
        AddParameter("Нижний:", _low);
        AddParameter("Порог / верхний:", _high);
        AddParameter("Линии:", _line);
        _parameters.Controls.Add(_parallel);
        _parameters.Controls.Add(_directions);
        Controls.Add(_parameters);
        Controls.SetChildIndex(imagesPanel, 0);
        Controls.SetChildIndex(_parameters, 1);
        Controls.SetChildIndex(toolbar, 2);
        Controls.SetChildIndex(statusStrip, 3);
        imagesPanel.RowCount = 3;
        imagesPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 220));
        imagesPanel.Controls.Add(_beforeHistogram, 0, 2);
        imagesPanel.Controls.Add(_afterHistogram, 1, 2);
        ClientSize = new Size(1120, 760);
        MinimumSize = new Size(1000, 720);
        loadProgressBar.Style = ProgressBarStyle.Continuous;
        UpdateParameters();
    }

    private void AddParameter(string text, params Control[] controls)
    {
        var group = new FlowLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = false, Margin = new(3, 3, 6, 3) };
        group.Controls.Add(new Label { Text = text, AutoSize = true, Margin = new(0, 6, 3, 0) });
        group.Controls.AddRange(controls);
        _parameters.Controls.Add(group);
    }

    private void UpdateParameters()
    {
        var method = (ProcessingMethod)methodComboBox.SelectedIndex;
        _radius.Parent!.Visible = method == ProcessingMethod.Sauvola;
        _k.Parent!.Visible = method == ProcessingMethod.Sauvola;
        _sigma.Parent!.Visible = method == ProcessingMethod.Canny;
        _low.Parent!.Visible = method == ProcessingMethod.Canny;
        _high.Parent!.Visible = method >= ProcessingMethod.Points;
        _line.Parent!.Visible = method == ProcessingMethod.Lines;
        _border.Parent!.Visible = method >= ProcessingMethod.Sauvola;
        UpdateLayoutMetrics();
    }

    private async void OpenButton_Click(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Открыть изображение",
            Filter = "Изображения|*.bmp;*.png;*.jpg;*.jpeg;*.gif;*.tif;*.tiff|Все файлы|*.*",
            RestoreDirectory = true
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        SetBusy(true);
        statusLabel.Text = "Загрузка изображения...";
        try
        {
            var file = dialog.FileName;
            var loaded = await Task.Run(() =>
            {
                var buffer = BitmapPixelAccess.Load(file);
                var histogram = ImageHistogram.Build(GrayscaleConverter.Convert(buffer));
                return (Buffer: buffer, Preview: BitmapPixelAccess.ToBitmap(buffer), Histogram: histogram);
            });
            if (IsDisposed || Disposing) { loaded.Preview.Dispose(); return; }
            _sourceBuffer = loaded.Buffer;
            SetImage(sourcePictureBox, loaded.Preview);
            ClearResult();
            _beforeHistogram.SetHistogram(loaded.Histogram);
            statusLabel.Text = $"{Path.GetFileName(file)} — {loaded.Buffer.Width} × {loaded.Buffer.Height} пикселей";
        }
        catch (Exception error) when (error is ArgumentException or IOException or UnauthorizedAccessException or ExternalException or OutOfMemoryException)
        { ShowError("Ошибка загрузки", error); }
        finally { if (!IsDisposed && !Disposing) SetBusy(false); }
    }

    private async void ProcessButton_Click(object? sender, EventArgs e)
    {
        if (_sourceBuffer is null || _busy) return;
        var settings = new ProcessingSettings
        {
            Method = (ProcessingMethod)methodComboBox.SelectedIndex,
            Radius = _radius.Value, K = (double)_k.Value, Sigma = (double)_sigma.Value,
            LowThreshold = (double)_low.Value, HighThreshold = (double)_high.Value,
            EdgeMode = _border.SelectedIndex == 0 ? EdgeMode.Clamp : EdgeMode.Reflect,
            LineDirection = (LineDirection)_line.SelectedIndex, Parallel = _parallel.Checked
        };
        if (settings.Method == ProcessingMethod.Canny && settings.LowThreshold > settings.HighThreshold)
        {
            MessageBox.Show(this, "Нижний порог не должен превышать верхний.", "Параметры");
            return;
        }
        if (settings.Method == ProcessingMethod.Sobel)
            settings = settings with { LowThreshold = Math.Min(settings.LowThreshold, settings.HighThreshold) };
        var source = _sourceBuffer;
        var methodName = methodComboBox.Text;
        using var cancellation = new CancellationTokenSource();
        _cancellation = cancellation;
        SetBusy(true);
        loadProgressBar.Value = 0;
        statusLabel.Text = "Обработка изображения...";
        var watch = Stopwatch.StartNew();
        var progress = new Progress<int>(value =>
        {
            if (!IsDisposed && !Disposing && _busy) loadProgressBar.Value = value;
        });
        try
        {
            var processed = await Task.Run(() =>
            {
                var result = ImageProcessor.Process(source, settings, cancellation.Token, progress);
                cancellation.Token.ThrowIfCancellationRequested();
                var binary = BitmapPixelAccess.ToBitmap(result.Image);
                try
                {
                    return (Result: result, Binary: binary,
                        Directions: result.Directions is null ? null : BitmapPixelAccess.ToBitmap(result.Directions));
                }
                catch { binary.Dispose(); throw; }
            });
            if (IsDisposed || Disposing || cancellation.IsCancellationRequested)
            {
                processed.Binary.Dispose(); processed.Directions?.Dispose();
                if (!IsDisposed && !Disposing) statusLabel.Text = "Обработка отменена.";
                return;
            }
            ClearResult();
            _result = processed.Result;
            _binaryPreview = processed.Binary;
            _directionPreview = processed.Directions;
            resultPlaceholder.Visible = false;
            resultTitle.Text = methodName;
            thresholdLabel.Text = processed.Result.Threshold is int threshold ? $"Порог: {threshold}" : "Порог: параметры";
            _beforeHistogram.SetHistogram(_result.BeforeHistogram);
            _afterHistogram.SetHistogram(_result.AfterHistogram);
            ShowResult();
            statusLabel.Text = $"{methodName}. Время: {watch.Elapsed.TotalMilliseconds:0} мс." +
                (settings.Method == ProcessingMethod.Canny ? $" Точек границы: {_result.EdgePoints.Count}." : "");
        }
        catch (OperationCanceledException) { if (!IsDisposed && !Disposing) statusLabel.Text = "Обработка отменена."; }
        catch (Exception error) when (error is ArgumentException or ExternalException or OutOfMemoryException or OverflowException)
        { ShowError("Ошибка обработки", error); }
        finally
        {
            _cancellation = null;
            if (!IsDisposed && !Disposing) SetBusy(false);
        }
    }

    private void SaveButton_Click(object? sender, EventArgs e)
    {
        if (resultPictureBox.Image is null) return;
        using var dialog = new SaveFileDialog { Filter = "PNG|*.png", FileName = "result.png", DefaultExt = "png", AddExtension = true };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try { resultPictureBox.Image.Save(dialog.FileName, System.Drawing.Imaging.ImageFormat.Png); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or ExternalException)
        { ShowError("Ошибка сохранения", error); }
    }

    private void CsvButton_Click(object? sender, EventArgs e)
    {
        if (_result is null) return;
        using var dialog = new SaveFileDialog { Filter = "CSV|*.csv", FileName = "edges.csv", DefaultExt = "csv", AddExtension = true };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            using var writer = new StreamWriter(dialog.FileName, false, Encoding.UTF8);
            writer.WriteLine("x,y,angle_radians,strength");
            foreach (var point in _result.EdgePoints)
                writer.WriteLine(string.Join(",", new[] { point.X, point.Y, point.Angle, point.Strength }
                    .Select(value => value.ToString("G17", CultureInfo.InvariantCulture))));
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        { ShowError("Ошибка сохранения", error); }
    }

    private void ShowResult() => resultPictureBox.Image =
        _directions.Checked && _directionPreview is not null ? _directionPreview : _binaryPreview;

    private void ClearResult()
    {
        resultPictureBox.Image = null;
        _binaryPreview?.Dispose(); _directionPreview?.Dispose();
        _binaryPreview = null; _directionPreview = null; _result = null;
        resultPlaceholder.Visible = true;
        resultTitle.Text = "Результат";
        thresholdLabel.Text = "Порог: —";
        _directions.Checked = false;
        _directions.Enabled = false;
        _afterHistogram.SetHistogram(null);
    }

    private void ShowError(string title, Exception error)
    {
        if (IsDisposed || Disposing) return;
        statusLabel.Text = title;
        MessageBox.Show(this, error.Message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    private void SetBusy(bool busy)
    {
        _busy = busy;
        openButton.Enabled = !busy;
        methodComboBox.Enabled = !busy;
        _parameters.Enabled = !busy;
        processButton.Enabled = !busy && _sourceBuffer is not null;
        _cancel.Enabled = busy && _cancellation is not null;
        _save.Enabled = !busy && _result is not null;
        _csv.Enabled = !busy && _result?.Directions is not null && _result.EdgePoints.Count > 0;
        loadProgressBar.Visible = busy;
        UpdateParameters();
        _directions.Enabled = !busy && _directionPreview is not null;
    }

    private static void SetImage(PictureBox pictureBox, Bitmap? image)
    {
        var previous = pictureBox.Image;
        pictureBox.Image = image;
        previous?.Dispose();
    }
}
