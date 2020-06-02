using DotNetConsoleSdk.Component.CommandLine.CommandModel;
using System;
using System.Data;
using System.Linq;

namespace DotNetConsoleSdk.Component.CommandLine.Parsing
{
    public class ParameterSyntax
    {
        public static string OptionPrefix = "-";

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

        public (ParseError parseError, ParameterSyntax parameterSyntax) MatchSegment(
            StringComparison syntaxMatchingRule,
            MatchingParameters matchingParameters,
            string segment, 
            int position, 
            int index,
            string[] rightSegments, 
            int firstIndex)
        {
            var csp = CommandParameterSpecification;
            if (matchingParameters.Contains(csp.ParameterName))
                return (new ParseError($"parameter already defined: {csp}", position, index, CommandSpecification),this);

            if (csp.OptionName != null)
            {
                var optsyntax = $"{ParameterSyntax.OptionPrefix}{csp.OptionName}";
                // option
                return optsyntax.Equals(segment, syntaxMatchingRule) ?
                        (null, this)
                        : (new ParseError($"parameter mismatch. attempted: {optsyntax}, found: {segment}", position, index, CommandSpecification), this);
            }
            else
            {
                if (csp.Name != null)
                {
                    // named parameter
                    return (csp.Index == position && csp.Name.Equals(segment, syntaxMatchingRule) && rightSegments.Length>0 )?
                        (null,this)
                        : (new ParseError($"", position, index, CommandSpecification), this);
                } else
                {
                    // fixed parameter (no name)

                }
            }
            throw new ConstraintException();
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
