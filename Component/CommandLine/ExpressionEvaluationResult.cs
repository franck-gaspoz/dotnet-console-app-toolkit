using DotNetConsoleSdk.Component.CommandLine.Parsing;
using System;

namespace DotNetConsoleSdk.Component.CommandLine
{
    public class ExpressionEvaluationResult
    {
        public readonly string SyntaxError;
        public readonly object Result;
        public readonly int EvalResultCode;
        public readonly ParseResult ParseResult;
        public readonly Exception EvalError;

        public ExpressionEvaluationResult(
            string syntaxError,
            ParseResult parseResult, 
            object result, 
            int evalResultCode,
            Exception evalError)
        {
            SyntaxError = syntaxError;
            ParseResult = parseResult;
            Result = result;
            EvalResultCode = evalResultCode;
            EvalError = evalError;
        }
    }
}
