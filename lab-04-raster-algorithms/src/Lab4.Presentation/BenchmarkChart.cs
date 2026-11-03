using System.ComponentModel;
using Lab4.RasterData;
using Lab4.RasterEngine;

namespace Lab4.Presentation;

internal sealed class BenchmarkChart : Control
{
    private BenchmarkReport? _report;
    private HashSet<RasterMethod> _visibleMethods = [];

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public BenchmarkScenario Scenario { get; set; }

    public BenchmarkChart()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
        BackColor = Color.White;
        Dock = DockStyle.Fill;
    }

    public void ShowReport(BenchmarkReport report, IEnumerable<RasterMethod> visibleMethods)
    {
        _report = report;
        _visibleMethods = visibleMethods.ToHashSet();
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var title = Scenario switch
        {
            BenchmarkScenario.LineCount => "Время от количества отрезков",
            BenchmarkScenario.LineLength => "Время от длины отрезка по ведущей оси",
            _ => "Время от количества окружностей"
        };
        var textHeight = TextRenderer.MeasureText(e.Graphics, "0", Font).Height;
        var padding = LogicalToDeviceUnits(12);
        var rowHeight = textHeight + padding / 2;
        TextRenderer.DrawText(e.Graphics, title, Font, new Point(padding, padding), ForeColor);
        var points = _report?.Points.Where(p => p.Scenario == Scenario && _visibleMethods.Contains(p.Method)).ToArray() ?? [];
        if (points.Length == 0)
        {
            TextRenderer.DrawText(e.Graphics, _report is null ? "Нажмите «Запустить замеры»." : "Выберите хотя бы один метод.",
                Font, new Point(padding, padding + rowHeight * 2), Color.DimGray);
            return;
        }

        var legendX = padding;
        var legendY = padding + rowHeight;
        foreach (var method in points.Select(p => p.Method).Distinct())
        {
            var name = BenchmarkNames.Title(method);
            var width = TextRenderer.MeasureText(e.Graphics, name, Font).Width + padding * 3;
            if (legendX + width > Width - padding && legendX != padding)
            {
                legendX = padding;
                legendY += rowHeight;
            }
            using var brush = new SolidBrush(BenchmarkNames.Color(method));
            e.Graphics.FillRectangle(brush, legendX, legendY + textHeight / 3, padding, padding / 2);
            TextRenderer.DrawText(e.Graphics, name, Font, new Point(legendX + padding * 3 / 2, legendY), ForeColor);
            legendX += width;
        }

        var maximum = Math.Max(0.001, points.Max(p => p.SamplesMilliseconds.Max()) * 1.12);
        var labelWidth = TextRenderer.MeasureText(e.Graphics, maximum.ToString("0.00"), Font).Width;
        var plot = Rectangle.FromLTRB(labelWidth + padding * 2, legendY + rowHeight * 2,
            Width - padding * 3, Height - rowHeight * 3);
        if (plot.Width < 60 || plot.Height < 60)
            return;
        TextRenderer.DrawText(e.Graphics, "Время, мс", Font,
            new Point(padding, legendY + rowHeight), ForeColor);
        var xValues = points.Select(XValue).Distinct().Order().ToArray();
        var maxX = xValues[^1];
        int ScreenX(int x) => plot.Left + (int)Math.Round(plot.Width * (double)x / maxX);
        int ScreenY(double y) => plot.Bottom - (int)Math.Round(plot.Height * y / maximum);
        using var gridBrush = new SolidBrush(Color.Gainsboro);
        for (var i = 0; i <= 5; i++)
        {
            var value = maximum * i / 5;
            var y = ScreenY(value);
            e.Graphics.FillRectangle(gridBrush, plot.Left, y, plot.Width, LogicalToDeviceUnits(1));
            TextRenderer.DrawText(e.Graphics, value.ToString(maximum < 10 ? "0.00" : "0.0"), Font,
                new Rectangle(0, y - textHeight / 2, plot.Left - padding / 2, textHeight), ForeColor,
                TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }
        using var axisBrush = new SolidBrush(Color.DimGray);
        e.Graphics.FillRectangle(axisBrush, plot.Left, plot.Top, LogicalToDeviceUnits(1), plot.Height);
        e.Graphics.FillRectangle(axisBrush, plot.Left, plot.Bottom, plot.Width, LogicalToDeviceUnits(1));
        foreach (var value in xValues.Prepend(0))
        {
            var x = ScreenX(value);
            var label = value.ToString("N0");
            var width = TextRenderer.MeasureText(e.Graphics, label, Font).Width;
            TextRenderer.DrawText(e.Graphics, label, Font, new Point(x - width / 2, plot.Bottom + padding / 2), ForeColor);
        }
        var xTitle = Scenario == BenchmarkScenario.LineLength ? "Длина L = max(|Δx|, |Δy|), клеток" : "Количество фигур";
        TextRenderer.DrawText(e.Graphics, xTitle, Font, new Rectangle(plot.Left, plot.Bottom + rowHeight,
            plot.Width, rowHeight), ForeColor, TextFormatFlags.HorizontalCenter);

        var saved = e.Graphics.Save();
        e.Graphics.SetClip(Rectangle.Inflate(plot, padding / 2, padding / 2));
        foreach (var group in points.GroupBy(p => p.Method))
        {
            using var brush = new SolidBrush(BenchmarkNames.Color(group.Key));
            Point? previous = null;
            foreach (var point in group.OrderBy(XValue))
            {
                var x = ScreenX(XValue(point));
                var y = ScreenY(point.MedianMilliseconds);
                if (previous is { } start)
                    Connect(e.Graphics, brush, start, new Point(x, y));
                var top = ScreenY(point.SamplesMilliseconds.Max());
                var bottom = ScreenY(point.SamplesMilliseconds.Min());
                e.Graphics.FillRectangle(brush, x - 1, top, LogicalToDeviceUnits(2), Math.Max(1, bottom - top));
                var size = LogicalToDeviceUnits(6);
                e.Graphics.FillRectangle(brush, x - size / 2, y - size / 2, size, size);
                previous = new Point(x, y);
            }
        }
        e.Graphics.Restore(saved);
    }

    private static int XValue(BenchmarkPoint point) =>
        point.Scenario == BenchmarkScenario.LineLength ? point.LineLength : point.PrimitiveCount;

    // Графики тоже рисуются квадратами; готовые функции построения линий не используются.
    private static void Connect(Graphics graphics, Brush brush, Point start, Point end)
    {
        var steps = Math.Max(Math.Abs(end.X - start.X), Math.Abs(end.Y - start.Y));
        if (steps == 0)
            return;
        for (var i = 0; i <= steps; i++)
            graphics.FillRectangle(brush, start.X + (end.X - start.X) * i / steps,
                start.Y + (end.Y - start.Y) * i / steps, 2, 2);
    }
}

internal static class BenchmarkNames
{
    public static string Title(RasterMethod method) => method switch
    {
        RasterMethod.StepLine => "Пошаговый",
        RasterMethod.DdaLine => "ЦДА",
        RasterMethod.BresenhamLine => "Брезенхем",
        RasterMethod.WuLine => "Ву",
        RasterMethod.CastlePittewayLine => "Кастла–Питвея",
        _ => "Брезенхем — окружность"
    };

    public static Color Color(RasterMethod method) => method switch
    {
        RasterMethod.StepLine => System.Drawing.Color.FromArgb(45, 45, 45),
        RasterMethod.DdaLine => System.Drawing.Color.FromArgb(30, 105, 190),
        RasterMethod.BresenhamLine or RasterMethod.BresenhamCircle => System.Drawing.Color.FromArgb(30, 135, 65),
        RasterMethod.WuLine => System.Drawing.Color.FromArgb(210, 105, 15),
        _ => System.Drawing.Color.FromArgb(135, 55, 165)
    };
}
