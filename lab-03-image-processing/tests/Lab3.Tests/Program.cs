var failures = new List<string>();
var count = 0;

PixelAccessTests.Run(Check);

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
