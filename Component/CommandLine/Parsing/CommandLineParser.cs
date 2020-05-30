using DotNetConsoleSdk.Component.CommandLine.CommandModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DotNetConsoleSdk.Component.CommandLine.Parsing
{
    public static class CommandLineParser
    {
        public static StringComparison SyntaxMatchingRule = StringComparison.InvariantCultureIgnoreCase;

        public static string[] SplitExpr(string expr)
        {
            if (expr == null) return new string[] { };
            var splits = new List<string>();
            var t = expr.Trim().ToCharArray();
            var inQuotedStr = false;
            int i = 0;
            var curStr = "";
            char prevc = ' ';
            while (i < t.Length)
            {
                var c = t[i];
                if (!inQuotedStr)
                {
                    if (c == ' ')
                    {
                        splits.Add(curStr);
                        curStr = "";
                    }
                    else
                    {
                        if (c == '"')
                            inQuotedStr = true;
                        else
                            curStr += c;
                    }
                }
                else
                {
                    if (c == '"' && prevc != '\\')
                        inQuotedStr = false;
                    else
                        curStr += c;
                }
                prevc = c;
                i++;
            }
            if (!string.IsNullOrWhiteSpace(curStr))
                splits.Add(curStr);
            return splits.ToArray();
        }

        public static (ParseResult parseResult,CommandSpecification comSpec) 
            Parse(SyntaxAnalyser syntaxAnalyzer, string expr)
        {
            if (expr == null) return (ParseResult.Empty,null);
            expr = expr.Trim();
            if (string.IsNullOrEmpty(expr)) return (ParseResult.Empty,null);
            
            var splits = SplitExpr(expr);
            var token = splits.First();
            var ctokens = syntaxAnalyzer.FindSyntaxesFromToken(token, false, SyntaxMatchingRule);

            if (ctokens.Count == 0) return (ParseResult.NotIdentified, null);

            

            return (ParseResult.Valid,null);
        }
    }
}
