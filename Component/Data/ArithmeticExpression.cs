using System;
using System.Collections.Generic;
using System.Text;

namespace DotNetConsoleAppToolkit.Component.Data
{
    public class ArithmeticExpression
    {
        /*public bool IsArithmeticExpression(string s)
        {
            return false;
        }*/

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
