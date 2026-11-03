using System.Diagnostics;
using System.Runtime.InteropServices;
using Lab4.RasterEngine;

namespace Lab4.RasterData;

public static class RasterBenchmark
{
    private static readonly RasterMethod[] LineMethods =
    [
        RasterMethod.StepLine, RasterMethod.DdaLine, RasterMethod.BresenhamLine,
        RasterMethod.WuLine, RasterMethod.CastlePittewayLine
    ];

    private sealed record Job(BenchmarkScenario Scenario, RasterMethod Method, int Count, int Length,
        LineSegment[]? Lines, Circle[]? Circles);

    public static BenchmarkReport Run(BenchmarkOptions? options = null,
        IProgress<BenchmarkProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        options ??= new BenchmarkOptions();
        options.Validate();
        cancellationToken.ThrowIfCancellationRequested();
        var grid = new VirtualGrid(2 * options.MaximumLength + 1, 2 * options.MaximumLength + 1);
        var lines = BenchmarkWorkloads.CreateLines(options.LineCount, options.MaximumLength, options.Seed);
        var circles = BenchmarkWorkloads.CreateCircles(options.CircleCount, options.MaximumLength, options.Seed);
        var jobs = new List<Job>();
        foreach (var count in new[] { options.LineCount / 10, options.LineCount / 4,
                     options.LineCount / 2, options.LineCount }.Select(n => Math.Max(1, n)).Distinct())
            foreach (var method in LineMethods)
                jobs.Add(new Job(BenchmarkScenario.LineCount, method, count, options.MaximumLength, lines, null));
        foreach (var count in new[] { options.CircleCount / 10, options.CircleCount / 4,
                     options.CircleCount / 2, options.CircleCount }.Select(n => Math.Max(1, n)).Distinct())
            jobs.Add(new Job(BenchmarkScenario.CircleCount, RasterMethod.BresenhamCircle, count, 0, null, circles));
        foreach (var length in new[] { options.MaximumLength / 16, options.MaximumLength / 8,
                     options.MaximumLength / 4, options.MaximumLength / 2, options.MaximumLength }
                     .Select(n => Math.Max(2, n)).Distinct())
        {
            var fixedLines = BenchmarkWorkloads.CreateLines(options.LengthTestCount, length, options.Seed, true);
            foreach (var method in LineMethods)
                jobs.Add(new Job(BenchmarkScenario.LineLength, method, options.LengthTestCount, length, fixedLines, null));
        }

        var total = jobs.Count * options.Repetitions;
        progress?.Report(new BenchmarkProgress(0, total));
        // Прогрев JIT и матрицы выполняется до первого измерения.
        for (var round = 0; round < 3; round++)
        {
            foreach (var method in LineMethods)
                DrawBatch(new Job(BenchmarkScenario.LineCount, method, Math.Min(4096, lines.Length),
                    options.MaximumLength, lines, null), grid, cancellationToken);
            DrawBatch(new Job(BenchmarkScenario.CircleCount, RasterMethod.BresenhamCircle,
                Math.Min(4096, circles.Length), 0, null, circles), grid, cancellationToken);
        }

        var samples = jobs.Select(_ => new double[options.Repetitions]).ToArray();
        var populated = new int[jobs.Count];
        var completed = 0;
        for (var repeat = 0; repeat < options.Repetitions; repeat++)
        {
            // Сдвиг порядка уменьшает зависимость результата от очередности методов.
            for (var offset = 0; offset < jobs.Count; offset++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var index = (offset + repeat * 7) % jobs.Count;
                var job = jobs[index];
                grid.Clear(); // Очистка, генератор случайных чисел и UI не входят в таймер.
                var start = Stopwatch.GetTimestamp();
                DrawBatch(job, grid, cancellationToken);
                samples[index][repeat] = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
                populated[index] = grid.CountSetPixels();
                progress?.Report(new BenchmarkProgress(++completed, total, job.Method, job.Count, repeat + 1));
            }
        }

        var points = jobs.Select((job, i) => new BenchmarkPoint(job.Scenario, job.Method,
            job.Count, job.Length, samples[i], populated[i])).ToArray();
        return new BenchmarkReport(DateTimeOffset.Now, RuntimeInformation.FrameworkDescription,
            RuntimeInformation.OSDescription, RuntimeInformation.ProcessArchitecture.ToString(),
            Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER") ?? "Не указан",
            Environment.ProcessorCount, Stopwatch.Frequency, Stopwatch.IsHighResolution,
#if DEBUG
            true,
#else
            false,
#endif
            options, points);
    }

    private static void DrawBatch(Job job, VirtualGrid grid, CancellationToken cancellationToken)
    {
        for (var i = 0; i < job.Count; i++)
        {
            if ((i & 255) == 0)
                cancellationToken.ThrowIfCancellationRequested();
            if (job.Circles is not null)
            {
                var circle = job.Circles[i];
                RasterAlgorithms.BresenhamCircle(circle.X, circle.Y, circle.Radius, grid);
                continue;
            }
            var line = job.Lines![i];
            switch (job.Method)
            {
                case RasterMethod.StepLine:
                    RasterAlgorithms.StepLine(line.X0, line.Y0, line.X1, line.Y1, grid);
                    break;
                case RasterMethod.DdaLine:
                    RasterAlgorithms.DdaLine(line.X0, line.Y0, line.X1, line.Y1, grid);
                    break;
                case RasterMethod.BresenhamLine:
                    RasterAlgorithms.BresenhamLine(line.X0, line.Y0, line.X1, line.Y1, grid);
                    break;
                case RasterMethod.WuLine:
                    RasterAlgorithms.WuLine(line.X0, line.Y0, line.X1, line.Y1, grid);
                    break;
                case RasterMethod.CastlePittewayLine:
                    RasterAlgorithms.CastlePittewayLine(line.X0, line.Y0, line.X1, line.Y1, grid);
                    break;
            }
        }
    }
}
