using DotNetConsoleSdk.Component.CommandLine.CommandModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;

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

        public (MatchingParameters matchingParameters,List<ParseError> parseErrors) Match(
            StringComparison syntaxMatchingRule,
            string[] segments,
            int firstIndex=0)
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

            // segments must match one time some of the parameters
            var parseErrors = new List<ParseError>();
            var index = 0;
            for (int i=0;i<segments.Length;i++)
            {
                string[] rightSegments;
                if (i + 1 < segments.Length)
                    rightSegments = segments[(i + 1)..^1];
                else
                    rightSegments = new string[] { };

                var (parseError,parameterSyntax) = MatchSegment(
                    syntaxMatchingRule,
                    matchingParameters, 
                    segments[i], 
                    i,
                    index,
                    rightSegments, 
                    firstIndex);
                
                if (parseError != null)
                {
                    parseErrors.Add(parseError);
                } else
                {
                    var cps = parameterSyntax.CommandParameterSpecification;
                    var mparam = parameterSyntax.GetMatchingParameter(cps.DefaultValue);
                    if (cps.IsOption && !cps.HasValue )
                        mparam.SetValue(true);
                    else
                    {
                        if (parameterSyntax.TryGetValue(segments[i], out var cvalue))
                            mparam.SetValue(cvalue);
                        else
                            parseErrors.Add(new ParseError($"value '{segments[i]}' doesn't match parameter type: '{cps.ParameterInfo.ParameterType.Name}' ",i,index,CommandSpecification,cps));
                    }
                    matchingParameters.Add(
                        parameterSyntax.CommandParameterSpecification.ParameterName,
                        mparam
                        );
                }

                if (i > 0) index++;
                index += segments[i].Length;
            }

            // non given parameters must be optional
            // TODO: handle syntax 'ParamName ParamValue'
            foreach ( var psyx in _parameterSyntaxes)
            {
                var optionName = psyx.CommandParameterSpecification.OptionName;
                if (psyx.CommandParameterSpecification.IsOptional &&
                    !matchingParameters.Contains(psyx.CommandParameterSpecification.ParameterName))
                {
                    var mparam = psyx.GetMatchingParameter(psyx.CommandParameterSpecification.DefaultValue);
                    matchingParameters.Add(psyx.CommandParameterSpecification.ParameterName, mparam);
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

        (ParseError parseError,ParameterSyntax parameterSyntax) MatchSegment(
            StringComparison syntaxMatchingRule, 
            MatchingParameters matchingParameters, 
            string segment, 
            int position,
            int index,
            string[] rightSegments, 
            int firstIndex)
        {
            ParseError parseError = null;
            for (int i = 0; i < _parameterSyntaxes.Count; i++)
            {
                var (prsError,parameterSyntax) = _parameterSyntaxes[i].MatchSegment(syntaxMatchingRule, matchingParameters,segment, position, index, rightSegments, firstIndex);
                if (prsError == null) return (null, parameterSyntax);
                parseError = prsError;
            }
            return (parseError,null);
        }

        public object Invoke(MatchingParameters matchingParameters)
        {
            var parameters = new List<object>();
            foreach ( var parameter in CommandSpecification.MethodInfo.GetParameters() )
            {
                if (matchingParameters.TryGet(parameter.Name, out var matchingParameter))
                    parameters.Add(matchingParameter.GetValue());
                else 
                    throw new System.InvalidOperationException($"parameter not found: '{parameter.Name}' when invoking command: {CommandSpecification}");
            }
            var r = CommandSpecification.MethodInfo
                .Invoke(CommandSpecification.MethodOwner, parameters.ToArray());
            return r;
        }

        public override string ToString()
        {
            return CommandSpecification.ToString();
        }
    }
}
