namespace DotNetConsoleAppToolkit.Console
{
    public class LineSplit
    {
        public readonly string Text;
        public readonly int X;
        public readonly int Y;
        public readonly int Length;

        public LineSplit(string text,int x,int y,int length)
        {
            Text = text;
            X = x;
            Y = y;
            Length = length;
        }

        public override string ToString()
        {
            return $"pos={X},{Y} l={Length} Text={Text}";
        }
    }
}
