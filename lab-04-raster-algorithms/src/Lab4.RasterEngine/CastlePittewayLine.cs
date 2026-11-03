namespace Lab4.RasterEngine;

public static partial class RasterAlgorithms
{
    public static void CastlePittewayLine(int x0, int y0, int x1, int y1, IRasterTarget target)
    {
        ArgumentNullException.ThrowIfNull(target);
        var dx = Math.Abs((long)x1 - x0);
        var dy = Math.Abs((long)y1 - y0);
        var steep = dy > dx;
        var major = Math.Max(dx, dy);
        var minor = Math.Min(dx, dy);
        var stepX = Math.Sign((long)x1 - x0);
        var stepY = Math.Sign((long)y1 - y0);
        long x = x0;
        long y = y0;
        target.SetPixel(x0, y0, 1);

        if (minor == 0 || minor == major)
        {
            for (long i = 0; i < major; i++)
                Advance(minor == major);
            return;
        }

        var a = major - minor;
        var b = minor;
        var straight = new PatternPart(new PatternNode(false));
        var diagonal = new PatternPart(new PatternNode(true));
        while (a != b)
        {
            // Две симметричные подстановки Кастла–Питвея; a и b уменьшаются по Евклиду.
            if (a > b)
            {
                a -= b;
                diagonal = Join(straight, Reverse(diagonal));
            }
            else
            {
                b -= a;
                straight = Join(diagonal, Reverse(straight));
            }
        }

        var pattern = Join(diagonal, Reverse(straight));
        var stack = new Stack<PatternPart>();
        for (long repetition = 0; repetition < a; repetition++)
        {
            stack.Push(pattern);
            while (stack.Count > 0)
            {
                var part = stack.Pop();
                if (part.Node.IsLeaf)
                    Advance(part.Node.IsDiagonal);
                else
                {
                    var first = part.Reversed ? part.Node.Right : part.Node.Left;
                    var second = part.Reversed ? part.Node.Left : part.Node.Right;
                    stack.Push(second with { Reversed = second.Reversed ^ part.Reversed });
                    stack.Push(first with { Reversed = first.Reversed ^ part.Reversed });
                }
            }
        }

        void Advance(bool isDiagonal)
        {
            if (steep || isDiagonal)
                y += stepY;
            if (!steep || isDiagonal)
                x += stepX;
            target.SetPixel((int)x, (int)y, 1);
        }
    }

    // Храним конкатенации деревом, а разворот — флагом: длинные строки не копируются.
    private readonly record struct PatternPart(PatternNode Node, bool Reversed = false);

    private sealed class PatternNode
    {
        public bool IsLeaf { get; }
        public bool IsDiagonal { get; }
        public PatternPart Left { get; }
        public PatternPart Right { get; }

        public PatternNode(bool diagonal) { IsLeaf = true; IsDiagonal = diagonal; }
        public PatternNode(PatternPart left, PatternPart right) { Left = left; Right = right; }
    }

    private static PatternPart Join(PatternPart left, PatternPart right) => new(new PatternNode(left, right));
    private static PatternPart Reverse(PatternPart part) => part with { Reversed = !part.Reversed };
}
