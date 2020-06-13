using System;
using static DotNetConsoleAppToolkit.DotNetConsole;

namespace DotNetConsoleAppToolkit.Console
{
    public class TextColor
    {
        ConsoleColor? _foreground;
        public ConsoleColor? Foreground
        {
            get { return !_foreground.HasValue?_foreground.Value:DefaultForeground; }
            set { _foreground = value; }
        }

        ConsoleColor? _background;
        public ConsoleColor? Background
        {
            get { return !_background.HasValue?_background.Value:DefaultBackground;  }
            set { _background = value; }
        }

        public TextColor(ConsoleColor? foreground, ConsoleColor? background=null)
        {
            Foreground = foreground;
            Background = background;
        }

        public override string ToString()
        {
            return (!_foreground.HasValue ? "" : GetCmd(PrintDirectives.f + "", _foreground.Value.ToString().ToLower()))
                + (!_background.HasValue ? "" : GetCmd(PrintDirectives.b + "", _background.Value.ToString().ToLower()));
        }

        public static ConsoleColor GetColor(string colorName)
        {
            return (ConsoleColor)Enum.Parse(typeof(ConsoleColor), colorName);
        }
        
        public static ConsoleColor ParseColor(object c)
        {
            if (Enum.TryParse((string)c, true, out ConsoleColor r))
                return r;
            if (TraceCommandErrors) Error($"invalid color name: {c}");
            return DefaultForeground;
        }
    }
}
