using System.Globalization;
using System.Text;
using Lab4.RasterEngine;

namespace Lab4.RasterData;

public enum BenchmarkScenario { LineCount, LineLength, CircleCount }

public sealed record BenchmarkOptions
{
    public int LineCount { get; init; } = 100_000;
    public int CircleCount { get; init; } = 50_000;
    public int LengthTestCount { get; init; } = 20_000;
    public int MaximumLength { get; init; } = 256;
    public int Repetitions { get; init; } = 3;
    public int Seed { get; init; } = 9;

    public void Validate()
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(LineCount, 8);
        ArgumentOutOfRangeException.ThrowIfLessThan(CircleCount, 4);
        ArgumentOutOfRangeException.ThrowIfLessThan(LengthTestCount, 8);
        if (MaximumLength is < 16 or > 1024)
            throw new ArgumentOutOfRangeException(nameof(MaximumLength));
        if (Repetitions is < 1 or > 9)
            throw new ArgumentOutOfRangeException(nameof(Repetitions));
    }
}

public sealed record BenchmarkPoint(BenchmarkScenario Scenario, RasterMethod Method,
    int PrimitiveCount, int LineLength, double[] SamplesMilliseconds, int PopulatedPixels)
{
    public double MedianMilliseconds
    {
        get
        {
            var sorted = SamplesMilliseconds.Order().ToArray();
            return sorted.Length % 2 == 0
                ? (sorted[sorted.Length / 2 - 1] + sorted[sorted.Length / 2]) / 2
                : sorted[sorted.Length / 2];
        }
    }
}

public sealed record BenchmarkReport(DateTimeOffset MeasuredAt, string Runtime, string OperatingSystem,
    string Architecture, string Processor, int LogicalProcessors, long TimerFrequency,
    bool HighResolutionTimer, bool DebugBuild, BenchmarkOptions Options, BenchmarkPoint[] Points)
{
    public void SaveCsv(string path)
    {
        var text = new StringBuilder("Scenario;Method;PrimitiveCount;LineLength;MedianMs");
        for (var i = 0; i < Options.Repetitions; i++)
            text.Append($";Sample{i + 1}Ms");
        text.AppendLine(";PopulatedPixels;Seed;GridSize;MeasuredAt;Runtime;Architecture;TimerFrequency;DebugBuild");
        foreach (var point in Points)
        {
            text.Append($"{point.Scenario};{point.Method};{point.PrimitiveCount};{point.LineLength};");
            text.Append(point.MedianMilliseconds.ToString("F6", CultureInfo.InvariantCulture));
            foreach (var sample in point.SamplesMilliseconds)
                text.Append(';').Append(sample.ToString("F6", CultureInfo.InvariantCulture));
            text.AppendLine($";{point.PopulatedPixels};{Options.Seed};{2 * Options.MaximumLength + 1};" +
                $"{MeasuredAt:O};{Runtime};{Architecture};{TimerFrequency};{DebugBuild}");
        }
        File.WriteAllText(path, text.ToString(), new UTF8Encoding(true));
    }
}

public sealed record BenchmarkProgress(int Completed, int Total, RasterMethod? Method = null,
    int PrimitiveCount = 0, int Repetition = 0);
