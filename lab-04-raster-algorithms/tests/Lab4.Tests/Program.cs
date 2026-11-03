var suite = new TestSuite();
VirtualGridTests.Run(suite);
LineTests.Run(suite);
CircleTests.Run(suite);
WuTests.Run(suite);
CastlePittewayTests.Run(suite);
EllipseTests.Run(suite);
ViewportTests.Run(suite);
BenchmarkTests.Run(suite);

Console.WriteLine($"Проверок: {suite.Count}. Ошибок: {suite.Failures.Count}.");
foreach (var failure in suite.Failures)
    Console.Error.WriteLine($"ОШИБКА: {failure}");
return suite.Failures.Count == 0 ? 0 : 1;
