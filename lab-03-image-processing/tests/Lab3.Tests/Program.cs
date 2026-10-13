if (args.Length == 2 && args[0] == "--make-samples")
{
    SampleImages.Generate(args[1]);
    return 0;
}
if (args.Length == 3 && args[0] == "--report-data")
{
    SampleImages.Analyze(args[1], args[2]);
    return 0;
}

var failures = new List<string>();
var count = 0;

PixelAccessTests.Run(Check);
ThresholdTests.Run(Check);
IntegralImageTests.Run(Check);
ProcessingTests.Run(Check);

foreach (var failure in failures)
    Console.Error.WriteLine($"ОШИБКА: {failure}");

Console.WriteLine($"Проверок: {count}. Ошибок: {failures.Count}.");
return failures.Count == 0 ? 0 : 1;

void Check(bool condition, string description)
{
    count++;
    if (!condition)
        failures.Add(description);
}
