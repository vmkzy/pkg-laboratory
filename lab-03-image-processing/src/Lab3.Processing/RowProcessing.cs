namespace Lab3.Processing;

internal static class RowProcessing
{
    public static void For(int width, int height, bool parallel, CancellationToken token, Action<int> row)
    {
        token.ThrowIfCancellationRequested();
        if (parallel && (long)width * height >= 262144)
            Parallel.For(0, height, new ParallelOptions { CancellationToken = token }, row);
        else
            for (var y = 0; y < height; y++)
            {
                token.ThrowIfCancellationRequested();
                row(y);
            }
    }
}
