using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DotNetConsoleSdk.Component.CommandLine.Parsing
{
    public class MatchingParameters
    {
        Dictionary<string, IMatchingParameter> _parameters
            = new Dictionary<string, IMatchingParameter>();

        public ReadOnlyDictionary<string, IMatchingParameter> Parameters =>
            new ReadOnlyDictionary<string, IMatchingParameter>(_parameters);

        public bool Contains(string optionName) => _parameters.ContainsKey(optionName);

        public void Add(string optionName, IMatchingParameter matchingParameter)
        {
            _parameters.Add(optionName, matchingParameter);
        }
    }
}
