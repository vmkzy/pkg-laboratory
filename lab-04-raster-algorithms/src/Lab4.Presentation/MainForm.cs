using Lab4.RasterData;
using Lab4.RasterEngine;

namespace Lab4.Presentation;

public partial class MainForm : Form
{
    private readonly VirtualGrid _grid = new(81, 81);

    public MainForm()
    {
        InitializeComponent();
        methodComboBox.Items.AddRange([
            "Пошаговый алгоритм", "ЦДА", "Брезенхем — отрезок", "Брезенхем — окружность"
        ]);
        gridPainter.Grid = _grid;
        methodComboBox.SelectedIndexChanged += (_, _) => UpdateInputs();
        methodComboBox.FontChanged += (_, _) => FitMethodSelector();
        drawButton.Click += (_, _) => DrawRaster();
        clearButton.Click += (_, _) =>
        {
            _grid.Clear();
            gridPainter.Invalidate();
            statusLabel.Text = "Растр очищен.";
        };
        rootLayout.SizeChanged += (_, _) => UpdateTextWidth();
        Shown += (_, _) => FitMethodSelector();
        methodComboBox.SelectedIndex = 0;
        FitMethodSelector();
        UpdateTextWidth();
        DrawRaster();
    }

    private void UpdateInputs()
    {
        var circle = methodComboBox.SelectedIndex == 3;
        lineInputs.Visible = !circle;
        circleInputs.Visible = circle;
        statusLabel.Text = "Введите параметры и нажмите «Построить».";
    }

    private void DrawRaster()
    {
        var x0 = (int)x0Input.Value;
        var y0 = (int)y0Input.Value;
        var x1 = (int)x1Input.Value;
        var y1 = (int)y1Input.Value;
        _grid.Clear();
        switch (methodComboBox.SelectedIndex)
        {
            case 0:
                RasterAlgorithms.StepLine(x0, y0, x1, y1, _grid);
                break;
            case 1:
                RasterAlgorithms.DdaLine(x0, y0, x1, y1, _grid);
                break;
            case 2:
                RasterAlgorithms.BresenhamLine(x0, y0, x1, y1, _grid);
                break;
            case 3:
                RasterAlgorithms.BresenhamCircle((int)centerXInput.Value, (int)centerYInput.Value,
                    (int)radiusInput.Value, _grid);
                break;
            default:
                return;
        }

        gridPainter.Invalidate();
        var description = methodComboBox.SelectedIndex == 3
            ? $"Центр ({centerXInput.Value}; {centerYInput.Value}), радиус {radiusInput.Value}."
            : $"Отрезок ({x0}; {y0}) → ({x1}; {y1}).";
        var clipped = methodComboBox.SelectedIndex == 3 &&
            ((int)centerXInput.Value - radiusInput.Value < _grid.MinX ||
             (int)centerXInput.Value + radiusInput.Value > _grid.MaxX ||
             (int)centerYInput.Value - radiusInput.Value < _grid.MinY ||
             (int)centerYInput.Value + radiusInput.Value > _grid.MaxY);
        statusLabel.Text = $"{methodComboBox.SelectedItem}. {description} Пикселей в растре: {_grid.CountSetPixels()}." +
            (clipped ? " Часть окружности находится вне растра." : "");
    }

    private void FitMethodSelector()
    {
        var width = 0;
        using var graphics = methodComboBox.CreateGraphics();
        foreach (var item in methodComboBox.Items)
            width = Math.Max(width, TextRenderer.MeasureText(graphics, item.ToString(), methodComboBox.Font).Width);
        methodComboBox.Width = width + methodComboBox.PreferredHeight + LogicalToDeviceUnits(16);
        methodComboBox.DropDownWidth = methodComboBox.Width;
    }

    private void UpdateTextWidth()
    {
        var width = Math.Max(1, rootLayout.ClientSize.Width - rootLayout.Padding.Horizontal);
        hintLabel.MaximumSize = new Size(width, 0);
        statusLabel.MaximumSize = new Size(width, 0);
    }
}
