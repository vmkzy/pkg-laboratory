using System.Text.Json;
using System.Text.Json.Serialization;
using Lab4.RasterData;

if (args.Length == 2 && args[0] == "--examples")
{
    RasterExamples.Save(args[1]);
    return 0;
}
if (args.Length != 1)
{
    Console.WriteLine("Укажите путь результата: Lab4.BenchmarkRunner.exe results.json");
    return 1;
}

using var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cancellation.Cancel(); };
try
{
    var report = RasterBenchmark.Run(cancellationToken: cancellation.Token);
    var path = Path.GetFullPath(args[0]);
    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
    var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
    jsonOptions.Converters.Add(new JsonStringEnumConverter());
    File.WriteAllText(path, JsonSerializer.Serialize(report, jsonOptions));
    report.SaveCsv(Path.ChangeExtension(path, ".csv"));
    foreach (var point in report.Points.Where(p => p.Scenario != BenchmarkScenario.LineLength &&
                 p.PrimitiveCount == (p.Scenario == BenchmarkScenario.CircleCount
                     ? report.Options.CircleCount : report.Options.LineCount)))
        Console.WriteLine($"{point.Method,-24} {point.PrimitiveCount,7:N0}: {point.MedianMilliseconds,10:F3} мс");
    Console.WriteLine($"Сохранено: {path}");
    return 0;
}
catch (OperationCanceledException)
{
    Console.WriteLine("Измерение отменено.");
    return 2;
}
