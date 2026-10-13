# Лабораторная работа 3

Вариант 9, подгруппа 10А. C# / Windows Forms / .NET 10.

Оцу, итеративный порог, Саувола, точки, линии, Собель и Кэнни с субпиксельными координатами. Расчёты вручную; три слоя: Presentation, PixelAccess, Processing.

Открыть `Lab3.ImageProcessing.slnx`, запускаемый проект — `Lab3.Presentation`.

```powershell
dotnet run --project src/Lab3.Presentation
dotnet run --project tests/Lab3.Tests
dotnet publish src/Lab3.Presentation -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o dist/win-x64
```

Примеры — `samples`, отчёт — `docs/report.docx`, EXE после публикации — `dist/win-x64/Lab3.Presentation.exe`. Для точек и линий на примере 07 попробуйте порог 900.

Сауволу нужно сверить с алгоритмом из лекции. Сборка рассчитана на современную Windows x64, не на Windows XP.
