using DotNetConsoleSdk.Component.CommandLine.CommandModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;

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

                var (rparseErrors,parameterSyntax) = MatchSegment(
                    syntaxMatchingRule,
                    matchingParameters, 
                    segments[i], 
                    i,
                    index,
                    rightSegments, 
                    segments,
                    firstIndex);
                
                if (rparseErrors != null && rparseErrors.Count>0)
                {
                    parseErrors.AddRange(rparseErrors);
                } else
                {
                    var cps = parameterSyntax.CommandParameterSpecification;
                    var mparam = parameterSyntax.BuildMatchingParameter(cps.DefaultValue);
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
                    var mparam = psyx.BuildMatchingParameter(psyx.CommandParameterSpecification.DefaultValue);
                    matchingParameters.Add(psyx.CommandParameterSpecification.ParameterName, mparam);
                }
            }

            return (matchingParameters,parseErrors);
        }

        int MinAttemptedSegments => CommandSpecification.FixedParametersCount
            + CommandSpecification.RequiredOptionsCount;

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

        (List<ParseError> parseError,ParameterSyntax parameterSyntax) MatchSegment(
            StringComparison syntaxMatchingRule, 
            MatchingParameters matchingParameters, 
            string segment, 
            int position,
            int index,
            string[] rightSegments, 
            string[] segments,
            int firstIndex)
        {
            List<ParseError> parseErrors = new List<ParseError>();
            var cparamSytxs = new List<ParameterSyntax>();
            for (int i = 0; i < _parameterSyntaxes.Count; i++)
            {
                var (prsError,parameterSyntax) = _parameterSyntaxes[i].MatchSegment(syntaxMatchingRule, matchingParameters,segment, position, index, rightSegments, segments, firstIndex);
                if (prsError == null)
                    cparamSytxs.Add(parameterSyntax);
                if (prsError != null && prsError.Description!=null)
                {
                    if (!matchingParameters.Contains(_parameterSyntaxes[i].CommandParameterSpecification.ParameterName))
                        parseErrors.Add(prsError);
                }
            }
            if (cparamSytxs.Count == 0) return (parseErrors, null);
            if (cparamSytxs.Count==1) return (null, cparamSytxs.First());
            var optParamSytxs = cparamSytxs.Where(x => x.CommandParameterSpecification.IsOption).ToList();
            if (optParamSytxs.Count() == 1) return (null, optParamSytxs.First());
            var sb = new StringBuilder();
            sb.AppendLine($"command syntax is ambiguous. multiple parameters matches the segment '{segment}' at position {position},index {index}: ");
            for (int i = 0; i < cparamSytxs.Count(); i++)
                sb.AppendLine($"{i+"",2}. {cparamSytxs[i]}");
            parseErrors.Add(new ParseError(sb.ToString(), position, index, CommandSpecification));
            return (parseErrors, null);
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
