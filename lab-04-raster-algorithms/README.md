# Лабораторная работа 4

Подгруппа 10А. C# / Windows Forms / .NET 10.

Открыть `Lab4.RasterAlgorithms.slnx`, запускаемый проект — `Lab4.Presentation`.
Проверки — консольный проект `Lab4.Tests`.

Пошаговый алгоритм, ЦДА, Брезенхем (отрезок/окружность), Ву, Кастла–Питвея для отрезков и целочисленный эллипс. Алгоритмы заполняют виртуальный растр, интерфейс отображает его отдельно.

Колесо мыши — масштаб, перетаскивание — сдвиг, «Весь растр» — сброс вида. Под курсором показываются координаты и интенсивность клетки.

«Сравнить скорость» — замеры и три графика, выбор отображаемых методов, экспорт CSV. Для замеров выбрать Release.
Отчёт — `docs/report.docx`, результаты измерений — `docs/benchmark-results.json` и `.csv`.

```powershell
dotnet run --project tests/Lab4.Tests -c Release
dotnet run --project tools/Lab4.BenchmarkRunner -c Release -- docs/benchmark-results.json
dotnet publish src/Lab4.Presentation -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -o dist/win-x64
```

Готовый EXE: `dist/win-x64/Lab4.Presentation.exe`, установка .NET не нужна. Windows x64.
