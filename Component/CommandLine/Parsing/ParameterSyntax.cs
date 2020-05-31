using DotNetConsoleSdk.Component.CommandLine.CommandModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace DotNetConsoleSdk.Component.CommandLine.Parsing
{
    public class ParameterSyntax
    {
        readonly CommandParameterSpecification _commandParameterSpecification;

        public ParameterSyntax(CommandParameterSpecification commandParameterSpecification)
        {
            _commandParameterSpecification = commandParameterSpecification;
        }

        public ParseError MatchSegment(string segment, int position, string[] rightSegments, int firstIndex)
        {

            return null;
        }
    }
}
