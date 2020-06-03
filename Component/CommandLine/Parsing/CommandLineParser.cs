using DotNetConsoleSdk.Component.CommandLine.CommandModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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

        public static int GetIndex(int position,string expr)
        {
            var splits = SplitExpr(expr);
            var n = 0;
            for (int i = 0; i <= position && i<splits.Length; i++)
                n += splits[i].Length + ((i>0)?1:0);
            return n;
        }

        public static ParseResult Parse(SyntaxAnalyser syntaxAnalyzer, string expr)
        {
            if (expr == null) return new ParseResult(ParseResultType.Empty,null);
            
            expr = expr.Trim();
            if (string.IsNullOrEmpty(expr)) return new ParseResult(ParseResultType.Empty,null);
            
            var splits = SplitExpr(expr);
            var segments = splits.Skip(1).ToArray();
            var token = splits.First();
            var ctokens = syntaxAnalyzer.FindSyntaxesFromToken(token, false, SyntaxMatchingRule);

            if (ctokens.Count == 0) return new ParseResult(ParseResultType.NotIdentified,null);

            if (ctokens.Count > 0)
            {
                int nbValid = 0;
                var syntaxParsingResults = new List<CommandSyntaxParsingResult>();
                var validSyntaxParsingResults = new List<CommandSyntaxParsingResult>();

                foreach ( var syntax in ctokens )
                {
                    var (matchingParameters,parseErrors) = syntax.Match(SyntaxMatchingRule,segments, token.Length+1);
                    if (parseErrors.Count == 0)
                    {
                        nbValid++;
                        validSyntaxParsingResults.Add(new CommandSyntaxParsingResult(syntax, matchingParameters, parseErrors));
                    }
                    else
                        syntaxParsingResults.Add(new CommandSyntaxParsingResult(syntax, matchingParameters, parseErrors));
                }

                if (nbValid > 1)
                {
                    // try disambiguization : priority to com with the maximum of options
                    validSyntaxParsingResults.Sort(
                        new Comparison<CommandSyntaxParsingResult>((x, y)
                            => x.CommandSyntax.CommandSpecification.OptionsCount.CompareTo(
                                y.CommandSyntax.CommandSpecification.OptionsCount
                                )
                        ));
                    validSyntaxParsingResults.Reverse();
                    if (validSyntaxParsingResults[0].CommandSyntax.CommandSpecification.OptionsCount >
                        validSyntaxParsingResults[1].CommandSyntax.CommandSpecification.OptionsCount)
                    {
                        validSyntaxParsingResults = new List<CommandSyntaxParsingResult>
                        {
                            validSyntaxParsingResults.First()
                        };
                        nbValid = 1;
                    }
                    else
                        return new ParseResult(ParseResultType.Ambiguous, validSyntaxParsingResults);
                }

                if (nbValid == 0) return new ParseResult(ParseResultType.NotValid,syntaxParsingResults);
                
                if (nbValid == 1) return new ParseResult( ParseResultType.Valid, validSyntaxParsingResults );
            }
            throw new InvalidOperationException();
        }
    }
}
