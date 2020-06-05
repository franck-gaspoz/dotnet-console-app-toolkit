using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Linq;
using System;
using System.Globalization;

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
                commandParameterSpecifications.ToList().ForEach(x => _parametersSpecifications.Add(x.ActualName,x));
        }

        public string DeclaringTypeShortName
        {
            get
            {
                var r = MethodInfo.DeclaringType.Name;
                var i = r.LastIndexOf("Commands");
                if (i > 0)
                    r = r.Substring(0, i);
                return r;
            }
        }

        public string DeclaringTypeFullName => MethodInfo.DeclaringType.FullName;

        public int ParametersCount => _parametersSpecifications.Count;

        public int MinimumParametersCount => _fixedRequiredParametersCount + _requiredOptionsCount;

        public int OptionsCount
        {
            get
            {
                var n = 0;
                foreach (var p in _parametersSpecifications)
                    if (p.Value.IsOption) n++;
                return n;
            }
        }

        int _fixedRequiredParametersCount = -1;
        public int FixedParametersCount
        {
            get
            {
                if (_fixedRequiredParametersCount == -1)
                {
                    int n = 0;
                    foreach (var pspec in _parametersSpecifications.Values)
                        if (pspec.Index > -1 && !pspec.IsOptional) n++;
                    _fixedRequiredParametersCount = n;
                }
                return _fixedRequiredParametersCount;
            }
        }

        int _fixedOptionalParametersCount = -1;
        public int FixedOptionalParametersCount
        {
            get
            {
                if (_fixedOptionalParametersCount == -1)
                {
                    int n = 0;
                    foreach (var pspec in _parametersSpecifications.Values)
                        if (pspec.Index > -1 && pspec.IsOptional) n++;
                    _fixedOptionalParametersCount = n;
                }
                return _fixedOptionalParametersCount;
            }
        }

        int _requiredOptionsCount = -1;
        public int RequiredOptionsCount
        {
            get
            {
                if (_requiredOptionsCount==-1)
                {
                    int n = 0;
                    foreach (var pspec in _parametersSpecifications.Values)
                        if (!pspec.IsOptional && pspec.IsOption) n++;
                    _requiredOptionsCount = n;
                }
                return _requiredOptionsCount;
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

        public string ToColorizedString()
        {
            var r = $"{Name}";
            var parameters = new SortedList<int, string>();
            var maxIndex = 0;
            foreach (var p in _parametersSpecifications.Values)
                if (p.Index > -1)
                {
                    maxIndex = Math.Max(p.Index, maxIndex);
                    parameters.Add(p.Index, p.ToColorizedString());
                }
            foreach (var p in _parametersSpecifications.Values)
                if (p.Index == -1)
                    parameters.Add(++maxIndex, p.ToColorizedString());
            return r + ((parameters.Values.Count == 0) ? "" : (" " + string.Join(' ', parameters.Values)));
        }
    }
}
