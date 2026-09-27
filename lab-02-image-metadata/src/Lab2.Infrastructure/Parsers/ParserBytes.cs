namespace Lab2.Infrastructure.Parsers;

/// <summary>Small reads shared by the byte parsers. Image payloads remain in the stream.</summary>
internal static class ParserBytes
{
    public static async ValueTask<bool> ReadAsync(Stream stream, Memory<byte> target,
        CancellationToken cancellationToken)
    {
        var read = 0;
        while (read < target.Length)
        {
            var count = await stream.ReadAsync(target[read..], cancellationToken);
            if (count == 0) return false;
            read += count;
        }
        return true;
    }

    public static bool Skip(Stream stream, long count)
    {
        if (count < 0 || count > stream.Length - stream.Position) return false;
        stream.Seek(count, SeekOrigin.Current);
        return true;
    }
}
