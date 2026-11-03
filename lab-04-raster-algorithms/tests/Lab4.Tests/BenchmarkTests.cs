using Lab4.RasterData;
using Lab4.RasterEngine;

internal static class BenchmarkTests
{
    public static void Run(TestSuite suite)
    {
        var lines = BenchmarkWorkloads.CreateLines(100_000, 256, 9);
        var repeated = BenchmarkWorkloads.CreateLines(100_000, 256, 9);
        suite.Check(lines.SequenceEqual(repeated), "Веер воспроизводим при seed 9");
        var octants = new int[8];
        var inBounds = true;
        foreach (var line in lines)
        {
            var x = line.X1;
            var y = line.Y1;
            var octant = x > 0
                ? (y > 0 ? (x > y ? 0 : 1) : (x > -y ? 7 : 6))
                : (y > 0 ? (-x > y ? 3 : 2) : (-x > -y ? 4 : 5));
            octants[octant]++;
            inBounds &= line.X0 == 0 && line.Y0 == 0 && Math.Abs(x) <= 256 && Math.Abs(y) <= 256;
        }
        suite.Check(octants.All(n => n == 12_500), "В каждом октанте ровно 12 500 отрезков");
        suite.Check(inBounds, "Все 100 000 отрезков помещаются в виртуальный растр");
        var fixedLines = BenchmarkWorkloads.CreateLines(1000, 64, 9, true);
        suite.Check(fixedLines.All(l => Math.Max(Math.Abs(l.X1), Math.Abs(l.Y1)) == 64),
            "В опыте с длиной L ведущая координата имеет ровно L шагов");
        var circles = BenchmarkWorkloads.CreateCircles(50_000, 256, 9);
        suite.Check(circles.All(c => c.X == 0 && c.Y == 0 && c.Radius is >= 1 and <= 256),
            "Все 50 000 окружностей помещаются в растр");
        suite.Check(circles.Select(c => c.Radius).Distinct().Count() == 256, "Представлены разные радиусы 1..256");
        suite.Check(circles.SequenceEqual(BenchmarkWorkloads.CreateCircles(50_000, 256, 9)),
            "Окружности воспроизводимы");

        var options = new BenchmarkOptions
        {
            LineCount = 80, CircleCount = 40, LengthTestCount = 80, MaximumLength = 32, Repetitions = 3
        };
        var updates = new List<BenchmarkProgress>();
        var report = RasterBenchmark.Run(options, new InlineProgress(updates.Add));
        suite.Check(report.Points.Length == 49, "Созданы все 49 точек графиков");
        suite.Check(updates[^1].Completed == 147 && updates[^1].Total == 147,
            "Три повтора завершены для всех точек");
        suite.Check(report.Options == options && report.TimerFrequency > 0, "Результат содержит параметры и таймер");
        foreach (var point in report.Points)
        {
            suite.Check(point.SamplesMilliseconds.Length == 3 &&
                point.SamplesMilliseconds.All(t => double.IsFinite(t) && t >= 0),
                $"Корректные времена: {point.Scenario}, {point.Method}, {point.PrimitiveCount}, {point.LineLength}");
            suite.Check(point.PopulatedPixels > 0 && point.PopulatedPixels <= 65 * 65,
                $"Алгоритм действительно заполнил массив: {point.Method}");
            suite.Check(point.MedianMilliseconds == point.SamplesMilliseconds.Order().ElementAt(1),
                "Медиана равна среднему по порядку измерению");
        }
        var fullLines = report.Points.Where(p => p.Scenario == BenchmarkScenario.LineCount && p.PrimitiveCount == 80).ToArray();
        suite.Check(fullLines.Single(p => p.Method == RasterMethod.BresenhamLine).PopulatedPixels ==
            fullLines.Single(p => p.Method == RasterMethod.CastlePittewayLine).PopulatedPixels,
            "Кастла–Питвея и Брезенхем получили одинаковое число пикселей в веере");

        var path = Path.GetTempFileName();
        try
        {
            report.SaveCsv(path);
            var csv = File.ReadAllLines(path);
            suite.Check(csv.Length == 50, "CSV содержит заголовок и все результаты");
            suite.Check(csv.All(row => row.Split(';').Length == csv[0].Split(';').Length),
                "Число столбцов CSV одинаково в каждой строке");
            suite.Check(csv[0].Contains("Sample3Ms") && csv[0].Contains("TimerFrequency"),
                "CSV сохраняет отдельные повторы и условия измерения");
        }
        finally { File.Delete(path); }

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        suite.Throws<OperationCanceledException>(() => RasterBenchmark.Run(options,
            cancellationToken: cancellation.Token), "Заранее отменённый замер не начинается");
        using var runningCancellation = new CancellationTokenSource();
        suite.Throws<OperationCanceledException>(() => RasterBenchmark.Run(options,
            new InlineProgress(p => { if (p.Completed == 1) runningCancellation.Cancel(); }),
            runningCancellation.Token), "Замер отменяется между сериями");
        foreach (var invalid in new[]
                 {
                     options with { LineCount = 7 }, options with { CircleCount = 3 },
                     options with { LengthTestCount = 7 }, options with { MaximumLength = 15 },
                     options with { MaximumLength = 1025 }, options with { Repetitions = 0 },
                     options with { Repetitions = 10 }
                 })
            suite.Throws<ArgumentOutOfRangeException>(() => RasterBenchmark.Run(invalid), "Некорректные параметры замера");
    }

    private sealed class InlineProgress(Action<BenchmarkProgress> action) : IProgress<BenchmarkProgress>
    {
        public void Report(BenchmarkProgress value) => action(value);
    }
}
