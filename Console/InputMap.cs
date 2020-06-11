using System;

namespace DotNetConsoleSdk.Console
{
    public class InputMap
    {
        public readonly string Text;
        public readonly object Code;
        public readonly Func<string, bool> MatchInput;
        public readonly bool CaseSensitiveMatch;

        public InputMap(string text, object code,bool caseSensitiveMatch=false)
        {
            Text = text;
            Code = code;
            CaseSensitiveMatch = caseSensitiveMatch;
        }

        public InputMap(Func<string, bool> matchInput, object code)
        {
            MatchInput = matchInput;
            Code = code;
        }
    }
}
