using DotNetConsoleSdk.Component.CommandLine.CommandModel;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DotNetConsoleSdk.Component.CommandLine.Parsing
{
    public class CommandSyntax
    {
        public readonly CommandSpecification CommandSpecification;

        readonly List<ParameterSyntax> _parameterSyntaxes = new List<ParameterSyntax>();

        public CommandSyntax(CommandSpecification commandSpecification)
        {
            CommandSpecification = commandSpecification;
            foreach (var kvp in commandSpecification.ParametersSpecifications)
                _parameterSyntaxes.Add(new ParameterSyntax(commandSpecification,kvp.Value));
        }

        public (MatchingParameters matchingParameters,List<ParseError> parseErrors) Match(string[] segments,int firstIndex=0)
        {
            var matchingParameters = new MatchingParameters();
            
            if (segments.Length < MinAttemptedSegments)
                return (matchingParameters,
                    new List<ParseError>{ 
                        new ParseError(
                             $"missing parameter(s). minimum attempted is {MinAttemptedSegments}, founded {segments.Length}",
                             0,
                             firstIndex,
                             CommandSpecification,
                             AttemptedParameters(0)
                             )});

            // segments must match some of the parameters
            var parseErrors = new List<ParseError>();
            for (int i=0;i<segments.Length;i++)
            {
                string[] rightSegments;
                if (i + 1 < segments.Length)
                    rightSegments = segments[(i + 1)..^1];
                else
                    rightSegments = new string[] { };
                var parseError = MatchSegment(segments[i], i, rightSegments, firstIndex); ;
                if (parseError != null)
                {
                    parseErrors.Add(parseError);
                    break;
                }
            }

            // non given parameters must be optional
            // TODO: handle syntax 'ParamName ParamValue'
            foreach ( var psyx in _parameterSyntaxes)
            {
                var optionName = psyx.CommandParameterSpecification.OptionName;
                if (psyx.CommandParameterSpecification.IsOptional &&
                    !matchingParameters.Contains(optionName))
                {
                    var mparam = psyx.GetMatchingParameter(psyx.CommandParameterSpecification.DefaultValue);
                    matchingParameters.Add(optionName, mparam);
                }
            }

            return (matchingParameters,parseErrors);
        }

        int MinAttemptedSegments => CommandSpecification.FixedParametersCount
            + CommandSpecification.RequiredNamedParametersCount;

        List<CommandParameterSpecification> AttemptedParameters(int position)
        {
            var r = new List<CommandParameterSpecification>();
            var psyxs = _parameterSyntaxes.ToArray();  // TODO: ordered by position
            for (int i = 0; i < psyxs.Length; i++)
            {
                var psyx = psyxs[i];
                //if (pspec.Index==-1)
            }
            return r;
        } 

        ParseError MatchSegment(string segment, int position, string[] rightSegments, int firstIndex)
        {
            for (int i=0;i< _parameterSyntaxes.Count;i++)
            {
                var parseError = _parameterSyntaxes[i].MatchSegment(segment, position, rightSegments, firstIndex);
                if (parseError != null) return parseError;
            }
            return null;
        }

        public object Invoke(MatchingParameters matchingParameters)
        {
            var parameters = new object[] { };
            var r = CommandSpecification.MethodInfo
                .Invoke(CommandSpecification.MethodOwner, parameters);
            return r;
        }

        public override string ToString()
        {
            return CommandSpecification.ToString();
        }
    }
}
