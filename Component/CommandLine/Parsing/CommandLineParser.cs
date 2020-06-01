using DotNetConsoleSdk.Component.CommandLine.CommandModel;
using System;
using System.Collections.Generic;
using System.Linq;
using static DotNetConsoleSdk.DotNetConsole;

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

        public static (
            ParseResult parseResult,
            List<CommandSpecification> commandSpecifications,
            List<ParseError> parseErrors
            ) 
            Parse(SyntaxAnalyser syntaxAnalyzer, string expr)
        {
            if (expr == null) return (ParseResult.Empty,null,null);
            expr = expr.Trim();
            if (string.IsNullOrEmpty(expr)) return (ParseResult.Empty,null,null);
            
            var splits = SplitExpr(expr);
            var segments = splits.Skip(1).ToArray();
            var token = splits.First();
            var ctokens = syntaxAnalyzer.FindSyntaxesFromToken(token, false, SyntaxMatchingRule);

            if (ctokens.Count == 0) return (ParseResult.NotIdentified, null,null);

            if (ctokens.Count > 0)
            {
                int nbValid = 0;
                var totalParseErrors = new List<ParseError>();
                foreach ( var syntax in ctokens )
                {
                    var (matchingParameters,parseErrors) = syntax.Match(segments,token.Length+1);
                    if (parseErrors.Count == 0) 
                        nbValid++;
                    else
                        totalParseErrors.AddRange(parseErrors);
                }
                if (nbValid == 0)
                    return (ParseResult.NotValid, ctokens.Select(x => x.CommandSpecification).ToList(), totalParseErrors);
                if (nbValid > 1)
                    return (ParseResult.Ambiguous, ctokens.Select(x => x.CommandSpecification).ToList(), new List<ParseError>());
                if (nbValid == 1)
                    return (ParseResult.Valid, new List<CommandSpecification> { ctokens.First().CommandSpecification }, totalParseErrors);
            }
            throw new InvalidOperationException();
        }
    }
}
