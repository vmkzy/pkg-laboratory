using Lab2.Core.Models;

var failures = new List<string>();

Check(
    new ImageMetadata { FilePath = @"C:\images\sample.bmp" }.FileName == "sample.bmp",
    "Имя файла должно извлекаться из полного пути.");

Check(
    new ScanProgress(25, 10, 9, 1).Percentage == 40,
    "Процент выполнения должен вычисляться из числа обработанных файлов.");

if (failures.Count == 0)
{
    Console.WriteLine("Базовые проверки моделей пройдены.");
    return 0;
}

foreach (var failure in failures)
{
    Console.Error.WriteLine($"ОШИБКА: {failure}");
}

return 1;

void Check(bool condition, string message)
{
    if (!condition)
    {
        failures.Add(message);
    }
}
