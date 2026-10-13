namespace Lab3.Presentation;

public sealed class HistogramControl : Control
{
    private long[]? _bins;
    [System.ComponentModel.DefaultValue("Гистограмма")]
    public string Caption { get; set; } = "Гистограмма";

    public HistogramControl()
    {
        DoubleBuffered = true;
        BackColor = Color.White;
    }

    public void SetHistogram(long[]? bins)
    {
        if (bins is not null && (bins.Length != 256 || bins.Any(value => value < 0)))
            throw new ArgumentException("Гистограмма должна содержать 256 неотрицательных значений.", nameof(bins));
        _bins = bins is null ? null : (long[])bins.Clone();
        Invalidate();
    }

    public override Size GetPreferredSize(Size proposedSize)
    {
        using var graphics = CreateGraphics();
        var textHeight = TextRenderer.MeasureText(graphics, "Аg", Font, Size.Empty,
            TextFormatFlags.NoPadding).Height;
        return new Size(proposedSize.Width, textHeight * 9 + LogicalToDeviceUnits(12));
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        var padding = LogicalToDeviceUnits(8);
        var flags = TextFormatFlags.NoPadding;
        var originalTextHeight = TextRenderer.MeasureText(g, "Аg", Font, Size.Empty, flags).Height;
        // В тесном окне уменьшаются только подписи графика, а не весь интерфейс.
        var fontScale = Math.Clamp((Height - 2 * padding) / (6f * originalTextHeight), 0.5f, 1f);
        using var textFont = new Font(Font.FontFamily, Font.SizeInPoints * fontScale, Font.Style);
        var textHeight = TextRenderer.MeasureText(g, "Аg", textFont, Size.Empty, flags).Height;
        var captionWidth = Math.Max(1, Width - 2 * padding);
        var captionSize = TextRenderer.MeasureText(g, Caption, textFont,
            new Size(captionWidth, int.MaxValue), flags | TextFormatFlags.WordBreak);
        TextRenderer.DrawText(g, Caption, textFont,
            new Rectangle(padding, padding, captionWidth, captionSize.Height), Color.Black,
            flags | TextFormatFlags.WordBreak);

        var max = _bins?.Max() ?? 0;
        var maxLabel = max.ToString("N0");
        var axisWidth = TextRenderer.MeasureText(g, maxLabel, textFont, Size.Empty, flags).Width;
        var left = padding + axisWidth + padding;
        var top = padding + captionSize.Height + padding + textHeight / 2;
        var showAxisName = Height >= textHeight * 8 + 2 * padding;
        var bottom = Height - padding - (showAxisName ? 2 : 1) * textHeight - padding / 2;
        var plot = Rectangle.FromLTRB(left, top, Width - padding, bottom);
        if (plot.Width <= 0 || plot.Height <= 0) return;

        using var gridPen = new Pen(Color.FromArgb(225, 225, 225));
        for (var level = 1; level <= 4; level++)
        {
            var y = plot.Bottom - plot.Height * level / 4;
            g.DrawLine(gridPen, plot.Left, y, plot.Right, y);
        }
        if (max > 0 && _bins is not null)
        {
            var saved = g.Save();
            g.SetClip(plot);
            var step = plot.Width / 256f;
            var barWidth = Math.Max(step, LogicalToDeviceUnits(2));
            for (var i = 0; i < 256; i++)
            {
                var center = plot.Left + (i + 0.5f) * step;
                var height = (float)((double)_bins[i] / max * plot.Height);
                g.FillRectangle(Brushes.SteelBlue, center - barWidth / 2,
                    plot.Bottom - height, barWidth, height);
            }
            g.Restore(saved);
        }
        g.DrawLine(Pens.Gray, plot.Left, plot.Bottom, plot.Right, plot.Bottom);
        g.DrawLine(Pens.Gray, plot.Left, plot.Top, plot.Left, plot.Bottom);

        if (max > 0)
        {
            DrawCount(max, plot.Top);
            DrawCount(max / 2, plot.Top + plot.Height / 2);
        }
        DrawCount(0, plot.Bottom);
        foreach (var value in new[] { 0, 64, 128, 192, 255 })
        {
            var label = value.ToString();
            var width = TextRenderer.MeasureText(g, label, textFont, Size.Empty, flags).Width;
            var x = plot.Left + (value + 0.5f) * plot.Width / 256;
            g.DrawLine(Pens.Gray, x, plot.Bottom, x, plot.Bottom + LogicalToDeviceUnits(3));
            var labelLeft = Math.Clamp((int)(x - width / 2f), padding,
                Math.Max(padding, Width - padding - width));
            TextRenderer.DrawText(g, label, textFont,
                new Rectangle(labelLeft, plot.Bottom + padding / 2, width, textHeight), Color.DimGray, flags);
        }
        if (showAxisName)
            TextRenderer.DrawText(g, "Яркость", textFont,
                new Rectangle(plot.Left, Height - padding - textHeight, plot.Width, textHeight), Color.DimGray,
                flags | TextFormatFlags.HorizontalCenter);

        void DrawCount(long count, int y) => TextRenderer.DrawText(g, count.ToString("N0"), textFont,
            new Rectangle(padding, y - textHeight / 2, axisWidth, textHeight), Color.DimGray,
            flags | TextFormatFlags.Right);
    }
}
