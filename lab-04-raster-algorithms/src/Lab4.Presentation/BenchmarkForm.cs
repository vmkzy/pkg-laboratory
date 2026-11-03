using Lab4.RasterData;
using Lab4.RasterEngine;

namespace Lab4.Presentation;

internal sealed class BenchmarkForm : Form
{
    private readonly Button _runButton = new() { Text = "Запустить замеры", AutoSize = true };
    private readonly Button _cancelButton = new() { Text = "Отмена", AutoSize = true, Enabled = false };
    private readonly Button _exportButton = new() { Text = "Сохранить CSV", AutoSize = true, Enabled = false };
    private readonly ProgressBar _progress = new() { Dock = DockStyle.Fill, Height = 22 };
    private readonly Label _status = new() { AutoSize = true, Dock = DockStyle.Fill, Padding = new Padding(4) };
    private readonly DataGridView _table = new()
    {
        Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells,
        RowHeadersVisible = false, BackgroundColor = SystemColors.Window
    };
    private readonly List<BenchmarkChart> _charts = [];
    private readonly Dictionary<RasterMethod, CheckBox> _methodChecks = [];
    private CancellationTokenSource? _cancellation;
    private BenchmarkReport? _report;
    private bool _closeWhenFinished;

    public BenchmarkForm()
    {
        AutoScaleDimensions = new SizeF(96, 96);
        AutoScaleMode = AutoScaleMode.Dpi;
        Font = new Font("Segoe UI", 10);
        Text = "Сравнение скорости растровых алгоритмов";
        ClientSize = new Size(1040, 740);
        MinimumSize = new Size(880, 620);
        StartPosition = FormStartPosition.CenterParent;
        var toolbar = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, WrapContents = true };
        toolbar.Controls.AddRange([_runButton, _cancelButton, _exportButton]);
        var explanation = new Label
        {
            AutoSize = true, Dock = DockStyle.Fill, Padding = new Padding(4, 4, 4, 8),
            Text = "100 000 отрезков · 50 000 окружностей · растр 513 × 513 · seed 9.\n" +
                "Три повтора после прогрева. Только заполнение массива, без UI, генератора и очистки. " +
                "На графиках — медиана и разброс min/max."
        };
        var methods = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, WrapContents = true };
        methods.Controls.Add(new Label { Text = "На графике:", AutoSize = true, Padding = new Padding(0, 5, 0, 0) });
        foreach (var method in new[] { RasterMethod.StepLine, RasterMethod.DdaLine, RasterMethod.BresenhamLine,
                     RasterMethod.WuLine, RasterMethod.CastlePittewayLine })
        {
            var check = new CheckBox { Text = BenchmarkNames.Title(method), AutoSize = true, Checked = true };
            check.CheckedChanged += (_, _) => UpdateCharts();
            _methodChecks.Add(method, check);
            methods.Controls.Add(check);
        }
        var tabs = new TabControl { Dock = DockStyle.Fill };
        foreach (var (scenario, title) in new[]
                 {
                     (BenchmarkScenario.LineCount, "Число отрезков"),
                     (BenchmarkScenario.LineLength, "Длина отрезка"),
                     (BenchmarkScenario.CircleCount, "Окружности")
                 })
        {
            var chart = new BenchmarkChart { Scenario = scenario };
            var page = new TabPage(title);
            page.Controls.Add(chart);
            tabs.TabPages.Add(page);
            _charts.Add(chart);
        }
        var tablePage = new TabPage("Все результаты");
        tablePage.Controls.Add(_table);
        tabs.TabPages.Add(tablePage);
        _table.Columns.Add("Scenario", "Опыт");
        _table.Columns.Add("Method", "Алгоритм");
        _table.Columns.Add("Count", "Фигур");
        _table.Columns.Add("Length", "Длина L");
        _table.Columns.Add("Median", "Медиана, мс");
        for (var i = 1; i <= 3; i++)
            _table.Columns.Add($"Sample{i}", $"Повтор {i}, мс");
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 7, Padding = new Padding(10) };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var i = 0; i < 7; i++)
            root.RowStyles.Add(new RowStyle(i == 4 ? SizeType.Percent : SizeType.AutoSize, i == 4 ? 100 : 0));
        root.Controls.Add(toolbar, 0, 0);
        root.Controls.Add(explanation, 0, 1);
        root.Controls.Add(methods, 0, 2);
        root.Controls.Add(_progress, 0, 3);
        root.Controls.Add(tabs, 0, 4);
        root.Controls.Add(_status, 0, 5);
        var releaseHint = new Label
        {
            Text = "Для сравнения используйте сборку Release. Во время замера лучше закрыть тяжёлые программы.",
            AutoSize = true, Dock = DockStyle.Fill, ForeColor = Color.DimGray
        };
        root.Controls.Add(releaseHint, 0, 6);
        Controls.Add(root);
        root.SizeChanged += (_, _) =>
        {
            var width = Math.Max(1, root.ClientSize.Width - root.Padding.Horizontal - 8);
            explanation.MaximumSize = new Size(width, 0);
            _status.MaximumSize = new Size(width, 0);
            releaseHint.MaximumSize = new Size(width, 0);
        };
        _status.Text = "Результаты появятся после замера. Данные можно сохранить в CSV.";
        _runButton.Click += async (_, _) => await RunBenchmarkAsync();
        _cancelButton.Click += (_, _) => _cancellation?.Cancel();
        _exportButton.Click += (_, _) => ExportCsv();
        FormClosing += (_, e) =>
        {
            if (_cancellation is not null)
            {
                _closeWhenFinished = true;
                _cancellation.Cancel();
                e.Cancel = true;
            }
        };
    }

    private async Task RunBenchmarkAsync()
    {
        if (_cancellation is not null)
            return;
        _cancellation = new CancellationTokenSource();
        _runButton.Enabled = false;
        _cancelButton.Enabled = true;
        _exportButton.Enabled = false;
        _progress.Value = 0;
        var progress = new Progress<BenchmarkProgress>(update =>
        {
            _progress.Maximum = update.Total;
            _progress.Value = update.Completed;
            _status.Text = update.Method is { } method
                ? $"{BenchmarkNames.Title(method)}: {update.PrimitiveCount:N0} фигур, повтор {update.Repetition}/3"
                : "Прогрев алгоритмов";
        });
        try
        {
            _report = await Task.Run(() => RasterBenchmark.Run(progress: progress,
                cancellationToken: _cancellation.Token));
            _table.Rows.Clear();
            foreach (var point in _report.Points)
            {
                var scenario = point.Scenario switch
                {
                    BenchmarkScenario.LineCount => "Число отрезков",
                    BenchmarkScenario.LineLength => "Длина отрезка",
                    _ => "Число окружностей"
                };
                _table.Rows.Add(scenario, BenchmarkNames.Title(point.Method), point.PrimitiveCount,
                    point.Scenario == BenchmarkScenario.CircleCount ? "—" : point.LineLength,
                    point.MedianMilliseconds.ToString("F3"),
                    point.SamplesMilliseconds[0].ToString("F3"), point.SamplesMilliseconds[1].ToString("F3"),
                    point.SamplesMilliseconds[2].ToString("F3"));
            }
            UpdateCharts();
            _status.Text = $"Готово. {_report.Runtime}, {_report.Architecture}, " +
                $"{(_report.DebugBuild ? "Debug" : "Release")}. Таймер: {_report.TimerFrequency:N0} тиков/с. " +
                "Снимите флажок медленного метода, чтобы подробнее сравнить остальные.";
        }
        catch (OperationCanceledException)
        {
            _status.Text = "Измерение отменено. Предыдущий завершённый результат сохранён.";
        }
        catch (Exception exception)
        {
            _status.Text = "Не удалось выполнить измерение: " + exception.Message;
        }
        finally
        {
            _cancellation.Dispose();
            _cancellation = null;
            _runButton.Enabled = true;
            _cancelButton.Enabled = false;
            _exportButton.Enabled = _report is not null;
            if (_closeWhenFinished)
                Close();
        }
    }

    private void UpdateCharts()
    {
        if (_report is null)
            return;
        var visible = _methodChecks.Where(p => p.Value.Checked).Select(p => p.Key)
            .Append(RasterMethod.BresenhamCircle).ToArray();
        foreach (var chart in _charts)
            chart.ShowReport(_report, visible);
    }

    private void ExportCsv()
    {
        if (_report is null)
            return;
        using var dialog = new SaveFileDialog { Filter = "CSV (*.csv)|*.csv", FileName = "lab4-benchmark.csv" };
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;
        try
        {
            _report.SaveCsv(dialog.FileName);
            _status.Text = "CSV сохранён: " + dialog.FileName;
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, exception.Message, "Не удалось сохранить CSV", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
