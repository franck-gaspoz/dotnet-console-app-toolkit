using DotNetConsoleSdk.Component.CommandLine.CommandModel;
using System;
using System.Collections.Generic;
using System.Linq;
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
                _parameterSyntaxes.Add(new ParameterSyntax(kvp.Value));
        }

        public List<ParseError> Match(string[] segments,int firstIndex=0)
        {
            if (segments.Length < MinAttemptedSegments)
                return new List<ParseError>{ 
                    new ParseError(
                         $"missing parameter(s). minimum attempted is {MinAttemptedSegments}, founded {segments.Length}",
                         0,
                         firstIndex,
                         CommandSpecification,
                         AttemptedParameters(0)
                         )};
            var r = new List<ParseError>();
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
                    r.Add(parseError);
                    break;
                }
            }
            return r;
        }

        int MinAttemptedSegments => CommandSpecification.FixedParametersCount
            + CommandSpecification.RequiredNamedParametersCount;

        List<CommandParameterSpecification> AttemptedParameters(int position)
        {
            var r = new List<CommandParameterSpecification>();
            var pspecs = CommandSpecification.ParametersSpecifications.Values.ToArray();
            for (int i = 0; i < pspecs.Length; i++)
            {
                var pspec = pspecs[i];
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

        public override string ToString()
        {
            return CommandSpecification.ToString();
        }
    }
}
