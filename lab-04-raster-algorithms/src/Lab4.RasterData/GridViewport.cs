namespace Lab4.RasterData;

public sealed class GridViewport
{
    public const int MinimumCellSize = 4;
    public const int MaximumCellSize = 64;

    public int CellSize { get; private set; } = 8;
    public double CenterX { get; private set; }
    public double CenterY { get; private set; }

    public (double X, double Y) ToScreen(double x, double y, int width, int height) =>
        (width / 2.0 + (x - CenterX) * CellSize, height / 2.0 - (y - CenterY) * CellSize);

    public (double X, double Y) ToWorld(double x, double y, int width, int height) =>
        (CenterX + (x - width / 2.0) / CellSize, CenterY - (y - height / 2.0) / CellSize);

    public void SetCellSize(int cellSize)
    {
        if (cellSize < MinimumCellSize || cellSize > MaximumCellSize)
            throw new ArgumentOutOfRangeException(nameof(cellSize));
        CellSize = cellSize;
    }

    public void ZoomAt(int cellSize, double x, double y, int width, int height)
    {
        var anchor = ToWorld(x, y, width, height);
        SetCellSize(cellSize);
        var moved = ToWorld(x, y, width, height);
        CenterX += anchor.X - moved.X;
        CenterY += anchor.Y - moved.Y;
    }

    public void Pan(double deltaX, double deltaY)
    {
        if (!double.IsFinite(deltaX) || !double.IsFinite(deltaY))
            throw new ArgumentOutOfRangeException(nameof(deltaX));
        CenterX -= deltaX / CellSize;
        CenterY += deltaY / CellSize;
    }

    public void Fit(VirtualGrid grid, int width, int height)
    {
        ArgumentNullException.ThrowIfNull(grid);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        CenterX = (grid.MinX + (double)grid.MaxX) / 2;
        CenterY = (grid.MinY + (double)grid.MaxY) / 2;
        CellSize = Math.Clamp(Math.Min(width / grid.Width, height / grid.Height), MinimumCellSize, MaximumCellSize);
    }
}
