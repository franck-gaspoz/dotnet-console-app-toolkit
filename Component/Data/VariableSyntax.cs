using System;
using System.Linq;

namespace DotNetConsoleAppToolkit.Component.Data
{
    public class VariableSyntax
    {
        public static int FindEndOfVariableName(char[] text,int beginPos)
        {
            int i = beginPos;
            while (i<text.Length)
            {
                if (!IsVariableNameValidCharacter(text[i]))
                {
                    break;
                }
                i++;
            }
            return i-1;
        }

        public static bool IsVariableNameValidCharacter(char c)
        {
            //return Char.IsLetterOrDigit(c);
            // shell not very restrictive
            return c > 32 && c!='"' && c!='\'' && c!='`' && c!='\\';
        }

        public static string[] SplitPath(string path)
        {
            return path?.Split('.');
        }

        public static string GetVariableName(string path)
        {
            if (path == null) return null;
            var p = SplitPath(path);
            if (p.Length == 0) return null;
            return p.Last();
        }

        public static string GetVariableName(ArraySegment<string> path)
        {
            if (path == null) return null;
            if (path.Count == 0) return null;
            return path.Last();
        }
    }
}
