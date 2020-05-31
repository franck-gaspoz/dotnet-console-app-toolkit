using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Linq;
using System;

namespace DotNetConsoleSdk.Component.CommandLine.CommandModel
{
    public class CommandSpecification
    {
        public readonly object MethodOwner;
        public readonly MethodInfo MethodInfo;
        public readonly string Description;
        public readonly string Name;

        readonly Dictionary<string, CommandParameterSpecification> _parametersSpecifications = new Dictionary<string, CommandParameterSpecification>();

        public ReadOnlyDictionary<string, CommandParameterSpecification> ParametersSpecifications => new ReadOnlyDictionary<string,CommandParameterSpecification>(_parametersSpecifications);

        public CommandSpecification(
            string name,
            string description,
            MethodInfo methodInfo, 
            object methodOwner,
            IList<CommandParameterSpecification> commandParameterSpecifications = null)
        {
            Name = name;
            Description = description;
            MethodOwner = methodOwner;
            MethodInfo = methodInfo;
            if (commandParameterSpecifications != null)
                commandParameterSpecifications.ToList().ForEach(x => _parametersSpecifications.Add(x.Name,x));
        }

        public object Invoke()
        {
            var parameters = new object[] { };
            var r = MethodInfo.Invoke(MethodOwner, parameters);
            return r;
        }

        public int ParametersCount => _parametersSpecifications.Count;

        public int MinimumParametersCount => _fixedParametersCount + _requiredNamedParametersCount;

        int _fixedParametersCount = -1;
        public int FixedParametersCount
        {
            get
            {
                if (_fixedParametersCount == -1)
                {
                    int n = 0;
                    foreach (var pspec in _parametersSpecifications.Values)
                        if (pspec.Index > -1) n++;
                    _fixedParametersCount = n;
                }
                return _fixedParametersCount;
            }
        }

        int _requiredNamedParametersCount = -1;
        public int RequiredNamedParametersCount
        {
            get
            {
                if (_requiredNamedParametersCount==-1)
                {
                    int n = 0;
                    foreach (var pspec in _parametersSpecifications.Values)
                        if (!pspec.IsOptional && pspec.Index == -1) n++;
                    _requiredNamedParametersCount = n;
                }
                return _requiredNamedParametersCount;
            }
        }

        public override string ToString()
        {
            var r = $"{Name}";
            var parameters = new SortedList<int, string>();
            var maxIndex = 0;
            foreach (var p in _parametersSpecifications.Values)
                if (p.Index > -1)
                {
                    maxIndex = Math.Max(p.Index, maxIndex);
                    parameters.Add(p.Index, p.ToString());
                }
            foreach (var p in _parametersSpecifications.Values)
                if (p.Index == -1)
                    parameters.Add(++maxIndex, p.ToString());
            return r+((parameters.Values.Count==0)?"":(" "+string.Join(' ',parameters.Values)));
        }
    }
}
