using System;

namespace DotNetConsoleSdk.Console
{
    public class ColorSettings
    {
        public static TextColor Log = new TextColor(ConsoleColor.Green, null);
        public static TextColor Error = new TextColor(ConsoleColor.Red, null);
        public static TextColor Warning = new TextColor(ConsoleColor.DarkYellow, null);

        public static TextColor Numeric = new TextColor(ConsoleColor.Cyan, null);
        public static TextColor Label = new TextColor(ConsoleColor.Cyan, null);
        public static TextColor Name = new TextColor(ConsoleColor.DarkYellow, null);
        public static TextColor DarkLabel = new TextColor(ConsoleColor.DarkCyan, null);
        public static TextColor HighlightIdentifier = new TextColor(ConsoleColor.Green, null);
        public static TextColor Highlight = new TextColor(ConsoleColor.Yellow, null);
        public static TextColor HalfDark = new TextColor(ConsoleColor.Gray, null);
        public static TextColor Dark = new TextColor(ConsoleColor.DarkGray, null);

        public static TextColor ParameterName = new TextColor(ConsoleColor.Yellow, null);
        public static TextColor ParameterValueType = new TextColor(ConsoleColor.DarkYellow, null);
        public static TextColor OptionPrefix = new TextColor(ConsoleColor.Yellow, null);
        public static TextColor OptionName = new TextColor(ConsoleColor.Yellow, null);
        public static TextColor SyntaxSymbol = new TextColor(ConsoleColor.Cyan, null);
    }
}
