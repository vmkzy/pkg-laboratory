using Lab4.RasterData;

internal static class ViewportTests
{
    public static void Run(TestSuite suite)
    {
        var viewport = new GridViewport();
        var grid = new VirtualGrid(81, 81);
        viewport.Fit(grid, 810, 648);
        suite.Check(viewport.CellSize == 8 && viewport.CenterX == 0 && viewport.CenterY == 0,
            "Масштаб: весь нечётный растр");
        var center = viewport.ToScreen(0, 0, 810, 648);
        suite.Check(center.X == 405 && center.Y == 324, "Начало координат в центре области");
        for (var x = -40; x <= 40; x += 5)
        {
            var screen = viewport.ToScreen(x, -x, 810, 648);
            var world = viewport.ToWorld(screen.X, screen.Y, 810, 648);
            suite.Check(world.X == x && world.Y == -x, "Прямое и обратное преобразование координат");
        }
        var before = viewport.ToScreen(4, 7, 810, 648);
        viewport.Pan(24, -16);
        var after = viewport.ToScreen(4, 7, 810, 648);
        suite.Check(after.X == before.X + 24 && after.Y == before.Y - 16, "Перетаскивание сдвигает изображение");
        var anchor = viewport.ToWorld(211, 87, 810, 648);
        viewport.ZoomAt(32, 211, 87, 810, 648);
        var zoomed = viewport.ToWorld(211, 87, 810, 648);
        suite.Check(Math.Abs(zoomed.X - anchor.X) < 1e-12 && Math.Abs(zoomed.Y - anchor.Y) < 1e-12,
            "Масштабирование не сдвигает точку под указателем");
        viewport.Fit(grid, 810, 648);
        suite.Check(viewport.CenterX == 0 && viewport.CenterY == 0 && viewport.CellSize == 8,
            "Весь растр сбрасывает масштаб и сдвиг");
        viewport.Fit(new VirtualGrid(4, 4), 100, 100);
        suite.Check(viewport.CenterX == -0.5 && viewport.CenterY == 0.5, "Центр чётного растра");
        viewport.Fit(grid, 1, 1);
        suite.Check(viewport.CellSize == GridViewport.MinimumCellSize, "Нижняя граница масштаба");
        viewport.Fit(grid, 100000, 100000);
        suite.Check(viewport.CellSize == GridViewport.MaximumCellSize, "Верхняя граница масштаба");
        suite.Throws<ArgumentOutOfRangeException>(() => viewport.SetCellSize(0), "Недопустимый масштаб");
        suite.Throws<ArgumentOutOfRangeException>(() => viewport.Pan(double.NaN, 0), "Недопустимый сдвиг");
        suite.Throws<ArgumentOutOfRangeException>(() => viewport.Fit(grid, 0, 100), "Пустая область просмотра");
    }
}
