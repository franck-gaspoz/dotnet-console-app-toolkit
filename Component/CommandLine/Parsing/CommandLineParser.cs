using DotNetConsoleAppToolkit.Component.Data;
using DotNetConsoleAppToolkit.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static DotNetConsoleAppToolkit.Component.Data.Variables;
using static DotNetConsoleAppToolkit.DotNetConsole;
using static DotNetConsoleAppToolkit.Lib.Str;

namespace DotNetConsoleAppToolkit.Component.CommandLine.Parsing
{
    public static class CommandLineParser
    {
        public static char VariablePrefixCharacter = '$';

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

        public static string SubstituteVariables(
            CommandEvaluationContext context,
            string expr
            )
        {
            var t = expr.ToCharArray();
            var i = 0;
            var vars = new List<StringSegment>();
            
            while (i<t.Length)
            {
                var c = t[i];
                if (c==VariablePrefixCharacter && (i==0 || t[i-1]!='\\' ))
                {
                    var j = VariableSyntax.FindEndOfVariableName(t, i+1);
                    var variable = expr.Substring(i+1, j - i);
                    vars.Add(new StringSegment(variable, i, j, j - i + 1));
                    i = j;
                }
                i++;
            }

            if (vars.Count > 0)
            {
                var nexpr = new StringBuilder();
                int x = 0;
                StringSegment lastvr = null; 
                foreach (var vr in vars)
                {
                    lastvr = vr;
                    nexpr.Append(expr.Substring(x, vr.X-x));
                    try
                    {
                        context.Variables.GetValue(vr.Text,out var value);
                        nexpr.Append(DoubleQuoteIfString(value.Value));
                    }
                    catch (VariableNotFoundException ex)
                    {
                        Errorln(ex.Message);
                        // keep bad var name in place
                        nexpr.Append("$" + vr.Text);
                    }
                    x = vr.Y + 1;
                }
                if (lastvr!=null)
                {
                    nexpr.Append(expr.Substring(x));
                }
                expr = nexpr.ToString();
            }

            return expr;
        }

        public static ParseResult Parse(
            CommandEvaluationContext context,
            SyntaxAnalyser syntaxAnalyzer, 
            string expr)
        {
            if (expr == null) return new ParseResult(ParseResultType.Empty,null);
            
            expr = expr.Trim();
            if (string.IsNullOrEmpty(expr)) return new ParseResult(ParseResultType.Empty,null);
            
            // -----> substitute variables values in commands args
            // -- TODO: do before 'FindSyntaxesFromToken', coz arg values are checked by command syntaxes
            expr = SubstituteVariables(context, expr);
            // <----------------------

            var splits = SplitExpr(expr);
            var segments = splits.Skip(1).ToArray();
            var token = splits.First();

            // get potential syntaxes
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

                if (nbValid == 1)
                {
                    #if NO
                    // -----> substitute variables values in commands args
                    // -- TODO: do before 'FindSyntaxesFromToken', coz arg values are checked by command syntaxes
                    var validSyntax = validSyntaxParsingResults.First();
                    foreach (var cmdParam in validSyntax.MatchingParameters.Parameters)
                        if (cmdParam.Value.GetValue() is string cmdParamValue && cmdParamValue!=null)
                            cmdParam.Value.SetValue(SubstituteVariables(context, cmdParamValue));
                    // <----------------------
                    #endif

                    return new ParseResult(ParseResultType.Valid, validSyntaxParsingResults);
                }
            }
            throw new InvalidOperationException();
        }
    }
}
