using System;

namespace DotNetConsoleAppToolkit.Console
{
    public class PrintSequence
    {
        public readonly PrintDirectives? PrintDirective;
        public readonly int FirstIndex;
        public readonly int LastIndex;
        public readonly string Value;
        public readonly string Text;
        public int Length => LastIndex - FirstIndex + 1;

        public PrintSequence(
            PrintDirectives? printDirective,
            int firstIndex,
            int lastIndex,
            string value,
            string text,
            int relIndex=0)
        {
            PrintDirective = printDirective;
            FirstIndex = firstIndex + relIndex;
            LastIndex = lastIndex + relIndex;
            Value = value;
            Text = text;
        }

        public PrintSequence(
            string printDirective,
            int firstIndex,
            int lastIndex,
            string value,
            string text,
            int relIndex=0)
        {
            if (printDirective != null)
                if (Enum.TryParse<PrintDirectives>(printDirective, out var pr))
                    PrintDirective = pr;

            FirstIndex = firstIndex+relIndex;
            LastIndex = lastIndex + relIndex;
            Value = value;
            Text = text;
        }

        public override string ToString()
        {
            var s = $"{FirstIndex}..{LastIndex}({Length})  ";
            if (PrintDirective.HasValue && Value == null)
                s += $"{PrintDirective}";
            else
            {
                if (PrintDirective.HasValue && Value != null)
                    s += $"{PrintDirective}={Value}";
                else
                    s += Text;
            }
            return s;
        }

        public string ToStringPattern()
        {
            var c = "-";
            var s = "";
            if (PrintDirective.HasValue && Value == null)
                s += $"{c}{PrintDirective}{c}";
            else
            {
                if (PrintDirective.HasValue && Value != null)
                    s += $"{c}{PrintDirective}={Value}{c}";
                else
                    s += Text;
            }
            return s;
        }
    }
}
