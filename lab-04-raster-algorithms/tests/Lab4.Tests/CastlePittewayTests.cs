using Lab4.RasterEngine;

internal static class CastlePittewayTests
{
    public static void Run(TestSuite suite)
    {
        for (var major = 2; major <= 32; major++)
        {
            for (var minor = 1; minor < major; minor++)
            {
                foreach (var (x, y) in new[]
                    { (major, minor), (minor, major), (-major, minor), (-minor, major),
                      (-major, -minor), (-minor, -major), (major, -minor), (minor, -major) })
                {
                    var structural = new RecordingTarget();
                    var incremental = new RecordingTarget();
                    RasterAlgorithms.CastlePittewayLine(-7, 4, x - 7, y + 4, structural);
                    RasterAlgorithms.BresenhamLine(-7, 4, x - 7, y + 4, incremental);
                    suite.Check(structural.Pixels.SequenceEqual(incremental.Pixels),
                        $"Кастла–Питвея: ({x},{y}), совпадение с Брезенхемом");
                }
            }
        }
    }
}
