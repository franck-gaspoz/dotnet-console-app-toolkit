using System;

namespace DotNetConsoleAppToolkit.Component.Data
{
    public class VariableSyntax
    {
        public static int FindEndOfVariableName(char[] text,int beginPos)
        {
            int i = beginPos;
            while (i<text.Length)
            {
                i++;
                if (!IsVariableNameValidCharacter(text[i]))
                    break;
            }
            return i-1;
        }

        public static bool IsVariableNameValidCharacter(char c)
        {
            return Char.IsLetterOrDigit(c);
        }
    }
}
