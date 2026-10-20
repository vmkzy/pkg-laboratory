using System.ComponentModel;
using Lab4.RasterData;

namespace Lab4.Presentation;

public sealed class GridPainter : Control
{
    private VirtualGrid? _grid;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public VirtualGrid? Grid
    {
        get => _grid;
        set { _grid = value; Invalidate(); }
    }

    public GridPainter()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (_grid is null)
            return;

        var graphics = e.Graphics;
        var gap = LogicalToDeviceUnits(10);
        var gridThickness = Math.Max(2, LogicalToDeviceUnits(1));
        var axisThickness = Math.Max(2, LogicalToDeviceUnits(2));
        var labelWidth = new[] { _grid.MinX, _grid.MaxX, _grid.MinY, _grid.MaxY }
            .Max(value => TextRenderer.MeasureText(graphics, value.ToString(), Font).Width);
        var textHeight = TextRenderer.MeasureText(graphics, "0", Font).Height;
        var left = labelWidth + gap * 2;
        var top = textHeight + gap;
        var bottom = textHeight * 2 + gap * 2;
        var right = labelWidth / 2 + gap;
        var cell = Math.Min((ClientSize.Width - left - right) / _grid.Width,
            (ClientSize.Height - top - bottom) / _grid.Height);
        if (cell < 1)
        {
            TextRenderer.DrawText(graphics, "Увеличьте окно для отображения растра.", Font,
                ClientRectangle, ForeColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            return;
        }

        var area = new Rectangle(left + (ClientSize.Width - left - right - cell * _grid.Width) / 2,
            top + (ClientSize.Height - top - bottom - cell * _grid.Height) / 2,
            cell * _grid.Width, cell * _grid.Height);
        using var background = new SolidBrush(Color.FromArgb(250, 251, 253));
        graphics.FillRectangle(background, area);

        // Оси проходят через центры клеток с координатами X=0 и Y=0.
        using var axis = new SolidBrush(Color.FromArgb(170, 113, 91));
        graphics.FillRectangle(axis, area.Left + (-_grid.MinX) * cell + cell / 2,
            area.Top, axisThickness, area.Height);
        graphics.FillRectangle(axis, area.Left,
            area.Top + _grid.MaxY * cell + cell / 2, area.Width, axisThickness);

        using var pixelBrush = new SolidBrush(Color.RoyalBlue);
        for (var y = _grid.MinY; y <= _grid.MaxY; y++)
        {
            for (var x = _grid.MinX; x <= _grid.MaxX; x++)
            {
                var intensity = _grid[x, y];
                if (intensity <= 0)
                    continue;
                pixelBrush.Color = Color.FromArgb((int)Math.Round(intensity * 255), 40, 102, 184);
                graphics.FillRectangle(pixelBrush, area.Left + (x - _grid.MinX) * cell,
                    area.Top + (_grid.MaxY - y) * cell, cell, cell);
            }
        }

        using var gridBrush = new SolidBrush(Color.FromArgb(216, 221, 229));
        if (cell >= 4)
        {
            for (var column = 0; column <= _grid.Width; column++)
                graphics.FillRectangle(gridBrush, area.Left + column * cell, area.Top, gridThickness, area.Height + gridThickness);
            for (var row = 0; row <= _grid.Height; row++)
                graphics.FillRectangle(gridBrush, area.Left, area.Top + row * cell, area.Width + gridThickness, gridThickness);
        }

        var tickStep = 1;
        while ((long)tickStep * cell < Math.Max(labelWidth + gap, textHeight * 2))
            tickStep = tickStep switch { 1 => 2, 2 => 5, _ => tickStep * 2 };

        for (var x = _grid.MinX; x <= _grid.MaxX; x++)
        {
            if (x % tickStep != 0)
                continue;
            var position = area.Left + (x - _grid.MinX) * cell + cell / 2;
            TextRenderer.DrawText(graphics, x.ToString(), Font,
                new Rectangle(position - labelWidth / 2, area.Bottom + gap / 2, labelWidth, textHeight),
                ForeColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPadding);
        }
        for (var y = _grid.MinY; y <= _grid.MaxY; y++)
        {
            if (y % tickStep != 0)
                continue;
            var position = area.Top + (_grid.MaxY - y) * cell + cell / 2;
            TextRenderer.DrawText(graphics, y.ToString(), Font,
                new Rectangle(area.Left - labelWidth - gap, position - textHeight / 2, labelWidth, textHeight),
                ForeColor, TextFormatFlags.Right | TextFormatFlags.NoPadding);
        }

        TextRenderer.DrawText(graphics, "X →", Font,
            new Point(area.Right - labelWidth, area.Bottom + textHeight + gap), ForeColor);
        TextRenderer.DrawText(graphics, "Y ↑", Font,
            new Point(area.Left - labelWidth - gap, area.Top - textHeight - gap / 2), ForeColor);
    }
}
