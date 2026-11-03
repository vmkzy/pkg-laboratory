using System.Text.Json;
using Lab4.RasterData;
using Lab4.RasterEngine;

internal static class RasterExamples
{
    public static void Save(string path)
    {
        var examples = new Dictionary<string, object>();
        Add("step", grid => RasterAlgorithms.StepLine(0, 0, 5, 3, grid));
        Add("dda", grid => RasterAlgorithms.DdaLine(0, 0, 5, 3, grid));
        Add("bresenham", grid => RasterAlgorithms.BresenhamLine(0, 0, 5, 3, grid));
        Add("wu", grid => RasterAlgorithms.WuLine(0, 0, 5, 3, grid));
        Add("circle", grid => RasterAlgorithms.BresenhamCircle(0, 0, 5, grid));
        Add("castle", grid => RasterAlgorithms.CastlePittewayLine(0, 0, 5, 3, grid));
        Add("ellipse", grid => RasterAlgorithms.MidpointEllipse(0, 0, 8, 4, grid));
        Add("fan", grid =>
        {
            foreach (var line in BenchmarkWorkloads.CreateLines(80, 10, 9))
                RasterAlgorithms.BresenhamLine(line.X0, line.Y0, line.X1, line.Y1, grid);
        });
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        File.WriteAllText(path, JsonSerializer.Serialize(examples));
        return;

        void Add(string name, Action<VirtualGrid> draw)
        {
            var grid = new VirtualGrid(23, 23);
            draw(grid);
            var pixels = new List<double[]>();
            for (var y = grid.MinY; y <= grid.MaxY; y++)
                for (var x = grid.MinX; x <= grid.MaxX; x++)
                    if (grid[x, y] > 0)
                        pixels.Add([x, y, grid[x, y]]);
            examples.Add(name, pixels);
        }
    }
}
