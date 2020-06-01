using DotNetConsoleSdk.Component.CommandLine.CommandModel;
using System.Collections.Generic;

namespace DotNetConsoleSdk.Component.CommandLine.Parsing
{
    public class ParseResult
    {
        public readonly ParseResultType ParseResultType;
        public readonly List<CommandSpecification> CommandSpecifications;
        public readonly List<ParseError> ParseErrors;

        public ParseResult(ParseResultType parseResultType, List<CommandSpecification> commandSpecifications, List<ParseError> parseErrors)
        {
            ParseResultType = parseResultType;
            CommandSpecifications = commandSpecifications;
            ParseErrors = parseErrors;
        }
    }
}
