using Lab4.RasterData;
using Lab4.RasterEngine;

internal static class VirtualGridTests
{
    public static void Run(TestSuite suite)
    {
        var grid = new VirtualGrid(5, 3);
        suite.Check(grid.MinX == -2 && grid.MaxX == 2 && grid.MinY == -1 && grid.MaxY == 1,
            "Границы нечётного растра");
        suite.Check(grid.CountSetPixels() == 0, "Новый растр пустой");
        grid.SetPixel(-2, 1, 1);
        grid.SetPixel(2, -1, 0.5);
        suite.Check(grid[-2, 1] == 1 && grid[2, -1] == 0.5 && grid[0, 0] == 0,
            "Отрицательные координаты и независимость клеток");
        grid.SetPixel(2, -1, 0.25);
        suite.Check(grid[2, -1] == 0.5, "Повторная запись не ослабляет интенсивность");
        grid.SetPixel(2, -1, 0.75);
        suite.Check(grid[2, -1] == 0.75, "Накопление максимальной интенсивности");
        grid.SetPixel(0, 0, 0);
        suite.Check(grid.CountSetPixels() == 2, "Нулевая интенсивность не закрашивает пиксель");
        grid.SetPixel(int.MaxValue, int.MinValue, 1);
        grid.SetPixel(3, 0, 1);
        grid.SetPixel(0, 2, 1);
        suite.Check(grid.CountSetPixels() == 2, "Записи вне растра пропускаются");
        grid.Clear();
        suite.Check(grid.CountSetPixels() == 0 && grid[-2, 1] == 0 && grid[2, -1] == 0,
            "Сброс всей матрицы");

        var even = new VirtualGrid(4, 4);
        suite.Check(even.MinX == -2 && even.MaxX == 1 && even.MinY == -1 && even.MaxY == 2,
            "Границы чётного растра");
        for (var y = even.MinY; y <= even.MaxY; y++)
            for (var x = even.MinX; x <= even.MaxX; x++)
                even.SetPixel(x, y, 1);
        suite.Check(even.CountSetPixels() == 16, "Все клетки чётного растра доступны");
        var single = new VirtualGrid(1, 1);
        single.SetPixel(0, 0, 1);
        suite.Check(single.CountSetPixels() == 1 && single[0, 0] == 1, "Растр из одного пикселя");

        suite.Throws<ArgumentOutOfRangeException>(() => new VirtualGrid(0, 1), "Нулевая ширина");
        suite.Throws<ArgumentOutOfRangeException>(() => new VirtualGrid(1, -1), "Отрицательная высота");
        suite.Throws<ArgumentOutOfRangeException>(() => _ = grid[3, 0], "Чтение вне растра");
        foreach (var intensity in new[] { -0.1, 1.1, double.NaN, double.PositiveInfinity, double.NegativeInfinity })
            suite.Throws<ArgumentOutOfRangeException>(() => grid.SetPixel(0, 0, intensity),
                $"Недопустимая интенсивность {intensity}");

        RasterAlgorithms.BresenhamLine(-20, 0, 20, 0, grid);
        suite.Check(grid.CountSetPixels() == 5, "Длинный отрезок обрезается только границами матрицы");
        var interfaceMethods = typeof(IRasterTarget).GetMethods();
        suite.Check(interfaceMethods.Length == 1 && interfaceMethods[0].Name == "SetPixel",
            "IRasterTarget содержит только SetPixel");
        foreach (var assembly in new[] { typeof(VirtualGrid).Assembly, typeof(RasterAlgorithms).Assembly })
            suite.Check(!assembly.GetReferencedAssemblies().Any(name =>
                name.Name?.Contains("Windows.Forms") == true || name.Name?.Contains("Drawing") == true),
                $"{assembly.GetName().Name} не зависит от графического интерфейса");
    }
}
