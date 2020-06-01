using DotNetConsoleSdk.Component.CommandLine.CommandModel;
using System;

namespace DotNetConsoleSdk.Component.CommandLine.Parsing
{
    public class ParameterSyntax
    {
        public readonly CommandSpecification CommandSpecification;
        public readonly CommandParameterSpecification CommandParameterSpecification;

        public ParameterSyntax(
            CommandSpecification commandSpecification,
            CommandParameterSpecification commandParameterSpecification
            )
        {
            CommandSpecification = commandSpecification;
            CommandParameterSpecification = commandParameterSpecification;
        }

        public ParseError MatchSegment(string segment, int position, string[] rightSegments, int firstIndex)
        {

            return null;
        }

        public IMatchingParameter GetMatchingParameter(object value)
        {
            var mparam = GetMatchingParameter();
            mparam.SetValue(value);
            return mparam;
        }

        public IMatchingParameter GetMatchingParameter()
        {
            IMatchingParameter mparam = null;
            var comspec = CommandParameterSpecification;
            var ptype = comspec.ParameterInfo.ParameterType;

            if (ptype == typeof(int))
                mparam = new MatchingParameter<int>(comspec);
            if (ptype == typeof(short))
                mparam = new MatchingParameter<short>(comspec);
            if (ptype == typeof(long))
                mparam = new MatchingParameter<long>(comspec);
            if (ptype == typeof(double))
                mparam = new MatchingParameter<double>(comspec);
            if (ptype == typeof(float))
                mparam = new MatchingParameter<float>(comspec);
            if (ptype == typeof(decimal))
                mparam = new MatchingParameter<decimal>(comspec);
            if (ptype == typeof(string))
                mparam = new MatchingParameter<string>(comspec);
            if (ptype == typeof(bool))
                mparam = new MatchingParameter<bool>(comspec);

            return mparam;

            throw new InvalidOperationException($"command parameter type not supported: {ptype.FullName} in command specification: {CommandParameterSpecification}");
        }
    }
}
