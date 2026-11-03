using Lab4.RasterData;
using Lab4.RasterEngine;

namespace Lab4.Presentation;

public partial class MainForm : Form
{
    private readonly VirtualGrid _grid = new(81, 81);
    private bool _updatingView;

    private sealed record MethodOption(RasterMethod Method, string Title)
    {
        public override string ToString() => Title;
    }

    public MainForm()
    {
        InitializeComponent();
        methodComboBox.Items.AddRange([
            new MethodOption(RasterMethod.StepLine, "Пошаговый алгоритм"),
            new MethodOption(RasterMethod.DdaLine, "ЦДА"),
            new MethodOption(RasterMethod.BresenhamLine, "Брезенхем — отрезок"),
            new MethodOption(RasterMethod.BresenhamCircle, "Брезенхем — окружность"),
            new MethodOption(RasterMethod.WuLine, "Ву — сглаженный отрезок"),
            new MethodOption(RasterMethod.CastlePittewayLine, "Кастла–Питвея — отрезок"),
            new MethodOption(RasterMethod.MidpointEllipse, "Эллипс — средняя точка")
        ]);
        gridPainter.Grid = _grid;
        methodComboBox.SelectedIndexChanged += (_, _) => UpdateInputs();
        methodComboBox.FontChanged += (_, _) => FitMethodSelector();
        drawButton.Click += (_, _) => DrawRaster();
        benchmarkButton.Click += (_, _) =>
        {
            using var form = new BenchmarkForm();
            form.ShowDialog(this);
        };
        clearButton.Click += (_, _) =>
        {
            _grid.Clear();
            gridPainter.Invalidate();
            statusLabel.Text = "Растр очищен.";
        };
        rootLayout.SizeChanged += (_, _) => UpdateTextWidth();
        Shown += (_, _) => { FitMethodSelector(); gridPainter.FitToGrid(); };
        fitButton.Click += (_, _) => gridPainter.FitToGrid();
        cellSizeInput.ValueChanged += (_, _) =>
        {
            if (!_updatingView)
                gridPainter.SetCellSize((int)cellSizeInput.Value);
        };
        gridPainter.ViewChanged += (_, _) =>
        {
            _updatingView = true;
            cellSizeInput.Value = gridPainter.CellSize;
            _updatingView = false;
        };
        showGridCheckBox.CheckedChanged += (_, _) => gridPainter.ShowGrid = showGridCheckBox.Checked;
        showAxesCheckBox.CheckedChanged += (_, _) => gridPainter.ShowAxes = showAxesCheckBox.Checked;
        gridPainter.PixelHovered += (x, y, intensity) => pixelInfoLabel.Text = $"X: {x}; Y: {y}; I: {intensity:0.###}";
        gridPainter.PixelLeft += (_, _) => pixelInfoLabel.Text = "Курсор вне растра";
        gridPainter.MouseLeave += (_, _) => pixelInfoLabel.Text = "Наведите курсор на клетку";
        DpiChanged += (_, _) => BeginInvoke((Action)(() => { FitMethodSelector(); gridPainter.FitToGrid(); }));
        methodComboBox.SelectedIndex = 0;
        FitMethodSelector();
        UpdateTextWidth();
        DrawRaster();
    }

    private void UpdateInputs()
    {
        if (methodComboBox.SelectedItem is not MethodOption option)
            return;
        var circle = option.Method == RasterMethod.BresenhamCircle;
        var ellipse = option.Method == RasterMethod.MidpointEllipse;
        lineInputs.Visible = !circle && !ellipse;
        circleInputs.Visible = circle || ellipse;
        radiusGroup.Visible = circle;
        radiusXGroup.Visible = ellipse;
        radiusYGroup.Visible = ellipse;
        var explanation = option.Method switch
        {
            RasterMethod.StepLine => "Координата вычисляется по уравнению прямой на каждом шаге.",
            RasterMethod.DdaLine => "Координаты накапливаются с постоянными приращениями.",
            RasterMethod.BresenhamLine => "Выбор пикселя по целочисленной ошибке.",
            RasterMethod.BresenhamCircle => "Один октант и симметрия относительно осей и диагоналей.",
            RasterMethod.WuLine => "Ву: дробная часть координаты определяет интенсивности двух соседних пикселей.",
            RasterMethod.CastlePittewayLine => "Кастла–Питвея: последовательность прямых и диагональных шагов строится по Евклиду.",
            _ => "Эллипс: целочисленный метод средней точки с двумя областями обхода."
        };
        hintLabel.Text = explanation + Environment.NewLine +
            "Растр 81 × 81, координаты от −40 до 40. Координата — центр клетки, Y направлена вверх. " +
            "Колесо — масштаб, перетаскивание — сдвиг.";
        statusLabel.Text = "Введите параметры и нажмите «Построить».";
    }

    private void DrawRaster()
    {
        if (methodComboBox.SelectedItem is not MethodOption option)
            return;
        var x0 = (int)x0Input.Value;
        var y0 = (int)y0Input.Value;
        var x1 = (int)x1Input.Value;
        var y1 = (int)y1Input.Value;
        _grid.Clear();
        switch (option.Method)
        {
            case RasterMethod.StepLine:
                RasterAlgorithms.StepLine(x0, y0, x1, y1, _grid);
                break;
            case RasterMethod.DdaLine:
                RasterAlgorithms.DdaLine(x0, y0, x1, y1, _grid);
                break;
            case RasterMethod.BresenhamLine:
                RasterAlgorithms.BresenhamLine(x0, y0, x1, y1, _grid);
                break;
            case RasterMethod.BresenhamCircle:
                RasterAlgorithms.BresenhamCircle((int)centerXInput.Value, (int)centerYInput.Value,
                    (int)radiusInput.Value, _grid);
                break;
            case RasterMethod.WuLine:
                RasterAlgorithms.WuLine(x0, y0, x1, y1, _grid);
                break;
            case RasterMethod.CastlePittewayLine:
                RasterAlgorithms.CastlePittewayLine(x0, y0, x1, y1, _grid);
                break;
            case RasterMethod.MidpointEllipse:
                RasterAlgorithms.MidpointEllipse((int)centerXInput.Value, (int)centerYInput.Value,
                    (int)radiusXInput.Value, (int)radiusYInput.Value, _grid);
                break;
            default:
                return;
        }

        gridPainter.Invalidate();
        var conic = option.Method is RasterMethod.BresenhamCircle or RasterMethod.MidpointEllipse;
        var description = option.Method switch
        {
            RasterMethod.BresenhamCircle => $"Центр ({centerXInput.Value}; {centerYInput.Value}), радиус {radiusInput.Value}.",
            RasterMethod.MidpointEllipse => $"Центр ({centerXInput.Value}; {centerYInput.Value}), A={radiusXInput.Value}, B={radiusYInput.Value}.",
            _ => $"Отрезок ({x0}; {y0}) → ({x1}; {y1})."
        };
        var extentX = option.Method == RasterMethod.MidpointEllipse ? radiusXInput.Value : radiusInput.Value;
        var extentY = option.Method == RasterMethod.MidpointEllipse ? radiusYInput.Value : radiusInput.Value;
        var clipped = conic &&
            (centerXInput.Value - extentX < _grid.MinX || centerXInput.Value + extentX > _grid.MaxX ||
             centerYInput.Value - extentY < _grid.MinY || centerYInput.Value + extentY > _grid.MaxY);
        statusLabel.Text = $"{methodComboBox.SelectedItem}. {description} Пикселей в растре: {_grid.CountSetPixels()}." +
            (clipped ? " Часть фигуры находится вне растра." : "");
    }

    private void FitMethodSelector()
    {
        var width = 0;
        using var graphics = methodComboBox.CreateGraphics();
        foreach (var item in methodComboBox.Items)
            width = Math.Max(width, TextRenderer.MeasureText(graphics, item.ToString(), methodComboBox.Font).Width);
        methodComboBox.Width = Math.Max(LogicalToDeviceUnits(280),
            width + methodComboBox.PreferredHeight + LogicalToDeviceUnits(32));
        methodComboBox.DropDownWidth = methodComboBox.Width;
    }

    private void UpdateTextWidth()
    {
        var width = Math.Max(1, rootLayout.ClientSize.Width - rootLayout.Padding.Horizontal);
        hintLabel.MaximumSize = new Size(width, 0);
        statusLabel.MaximumSize = new Size(width, 0);
    }
}
