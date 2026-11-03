using System.ComponentModel;
using Lab4.RasterData;

namespace Lab4.Presentation;

public sealed class GridPainter : Control
{
    private readonly GridViewport _viewport = new();
    private VirtualGrid? _grid;
    private bool _fitToWindow = true;
    private bool _panning;
    private Point _lastMouse;
    private bool _showGrid = true;
    private bool _showAxes = true;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public VirtualGrid? Grid
    {
        get => _grid;
        set { _grid = value; FitToGrid(); }
    }

    [DefaultValue(true)]
    public bool ShowGrid { get => _showGrid; set { _showGrid = value; Invalidate(); } }

    [DefaultValue(true)]
    public bool ShowAxes { get => _showAxes; set { _showAxes = value; Invalidate(); } }

    [Browsable(false)]
    public int CellSize => _viewport.CellSize;

    public event EventHandler? ViewChanged;
    public event Action<int, int, double>? PixelHovered;
    public event EventHandler? PixelLeft;

    public GridPainter()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
        TabStop = true;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true);
    }

    public void SetCellSize(int cellSize)
    {
        _viewport.SetCellSize(cellSize);
        _fitToWindow = false;
        ViewChanged?.Invoke(this, EventArgs.Empty);
        Invalidate();
    }

    public void FitToGrid()
    {
        _fitToWindow = true;
        if (_grid is null || ClientSize.Width <= 0 || ClientSize.Height <= 0)
            return;
        using var graphics = CreateGraphics();
        var plot = GetPlotArea(graphics);
        if (plot.Width > 0 && plot.Height > 0)
            _viewport.Fit(_grid, plot.Width, plot.Height);
        ViewChanged?.Invoke(this, EventArgs.Empty);
        Invalidate();
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (_fitToWindow)
            FitToGrid();
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);
        using var graphics = CreateGraphics();
        var plot = GetPlotArea(graphics);
        if (!plot.Contains(e.Location))
            return;
        var size = Math.Clamp(CellSize + Math.Sign(e.Delta) * Math.Max(1, CellSize / 5),
            GridViewport.MinimumCellSize, GridViewport.MaximumCellSize);
        _viewport.ZoomAt(size, e.X - plot.Left, e.Y - plot.Top, plot.Width, plot.Height);
        _fitToWindow = false;
        ViewChanged?.Invoke(this, EventArgs.Empty);
        Invalidate();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button is not (MouseButtons.Left or MouseButtons.Middle))
            return;
        Focus();
        _panning = true;
        _fitToWindow = false;
        _lastMouse = e.Location;
        Capture = true;
        Cursor = Cursors.SizeAll;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (_panning)
        {
            _viewport.Pan(e.X - _lastMouse.X, e.Y - _lastMouse.Y);
            _lastMouse = e.Location;
            Invalidate();
        }
        if (_grid is null)
            return;
        using var graphics = CreateGraphics();
        var plot = GetPlotArea(graphics);
        var raster = GetRasterArea(plot);
        if (plot.Contains(e.Location) && raster.Contains(e.Location))
        {
            var x = _grid.MinX + (e.X - raster.Left) / CellSize;
            var y = _grid.MaxY - (e.Y - raster.Top) / CellSize;
            PixelHovered?.Invoke(x, y, _grid[x, y]);
        }
        else
            PixelLeft?.Invoke(this, EventArgs.Empty);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        Capture = false;
    }

    protected override void OnMouseCaptureChanged(EventArgs e)
    {
        base.OnMouseCaptureChanged(e);
        _panning = false;
        Cursor = Cursors.Default;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (_grid is null)
            return;
        var graphics = e.Graphics;
        var plot = GetPlotArea(graphics);
        if (plot.Width < 1 || plot.Height < 1)
            return;
        var raster = GetRasterArea(plot);
        var gridThickness = Math.Max(2, LogicalToDeviceUnits(1));
        var axisThickness = Math.Max(2, LogicalToDeviceUnits(2));
        var saved = graphics.Save();
        graphics.SetClip(plot);
        using var outside = new SolidBrush(Color.FromArgb(241, 243, 246));
        using var background = new SolidBrush(Color.FromArgb(250, 251, 253));
        graphics.FillRectangle(outside, plot);
        graphics.FillRectangle(background, raster);

        if (ShowAxes)
        {
            using var axis = new SolidBrush(Color.FromArgb(170, 113, 91));
            graphics.FillRectangle(axis, raster.Left + (-_grid.MinX) * CellSize + CellSize / 2,
                raster.Top, axisThickness, raster.Height);
            graphics.FillRectangle(axis, raster.Left,
                raster.Top + _grid.MaxY * CellSize + CellSize / 2, raster.Width, axisThickness);
        }

        using var pixelBrush = new SolidBrush(Color.RoyalBlue);
        for (var y = _grid.MinY; y <= _grid.MaxY; y++)
        {
            for (var x = _grid.MinX; x <= _grid.MaxX; x++)
            {
                var intensity = _grid[x, y];
                if (intensity <= 0)
                    continue;
                pixelBrush.Color = Color.FromArgb((int)Math.Round(intensity * 255), 40, 102, 184);
                graphics.FillRectangle(pixelBrush, raster.Left + (x - _grid.MinX) * CellSize,
                    raster.Top + (_grid.MaxY - y) * CellSize, CellSize, CellSize);
            }
        }

        if (ShowGrid)
        {
            using var gridBrush = new SolidBrush(Color.FromArgb(216, 221, 229));
            for (var column = 0; column <= _grid.Width; column++)
                graphics.FillRectangle(gridBrush, raster.Left + column * CellSize, raster.Top,
                    gridThickness, raster.Height + gridThickness);
            for (var row = 0; row <= _grid.Height; row++)
                graphics.FillRectangle(gridBrush, raster.Left, raster.Top + row * CellSize,
                    raster.Width + gridThickness, gridThickness);
        }
        graphics.Restore(saved);
        DrawLabels(graphics, plot, raster);
    }

    private Rectangle GetPlotArea(Graphics graphics)
    {
        var gap = LogicalToDeviceUnits(10);
        var textHeight = TextRenderer.MeasureText(graphics, "0", Font).Height;
        var labelWidth = TextRenderer.MeasureText(graphics, "−40", Font).Width;
        return Rectangle.FromLTRB(labelWidth + gap * 2, textHeight + gap,
            ClientSize.Width - labelWidth / 2 - gap, ClientSize.Height - textHeight * 2 - gap * 2);
    }

    private Rectangle GetRasterArea(Rectangle plot)
    {
        if (_grid is null)
            return Rectangle.Empty;
        var corner = _viewport.ToScreen(_grid.MinX - 0.5, _grid.MaxY + 0.5, plot.Width, plot.Height);
        return new Rectangle(plot.Left + (int)Math.Round(corner.X), plot.Top + (int)Math.Round(corner.Y),
            _grid.Width * CellSize, _grid.Height * CellSize);
    }

    private void DrawLabels(Graphics graphics, Rectangle plot, Rectangle raster)
    {
        if (_grid is null)
            return;
        var gap = LogicalToDeviceUnits(10);
        var textHeight = TextRenderer.MeasureText(graphics, "0", Font).Height;
        var labelWidth = TextRenderer.MeasureText(graphics, "−40", Font).Width;
        var step = 1;
        while (step * CellSize < Math.Max(labelWidth + gap, textHeight * 2))
            step = step switch { 1 => 2, 2 => 5, _ => step * 2 };
        for (var x = _grid.MinX; x <= _grid.MaxX; x++)
        {
            var position = raster.Left + (x - _grid.MinX) * CellSize + CellSize / 2;
            if (x % step != 0 || position < plot.Left || position >= plot.Right)
                continue;
            TextRenderer.DrawText(graphics, x.ToString(), Font,
                new Rectangle(position - labelWidth / 2, plot.Bottom + gap / 2, labelWidth, textHeight),
                ForeColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPadding);
        }
        for (var y = _grid.MinY; y <= _grid.MaxY; y++)
        {
            var position = raster.Top + (_grid.MaxY - y) * CellSize + CellSize / 2;
            if (y % step != 0 || position < plot.Top || position >= plot.Bottom)
                continue;
            TextRenderer.DrawText(graphics, y.ToString(), Font,
                new Rectangle(plot.Left - labelWidth - gap, position - textHeight / 2, labelWidth, textHeight),
                ForeColor, TextFormatFlags.Right | TextFormatFlags.NoPadding);
        }
        TextRenderer.DrawText(graphics, "X →", Font,
            new Point(plot.Right - labelWidth, plot.Bottom + textHeight + gap), ForeColor);
        TextRenderer.DrawText(graphics, "Y ↑", Font,
            new Point(plot.Left - labelWidth - gap, plot.Top - textHeight - gap / 2), ForeColor);
    }
}
