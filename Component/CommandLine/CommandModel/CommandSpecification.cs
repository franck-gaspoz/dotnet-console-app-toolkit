﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Linq;

namespace DotNetConsoleSdk.Component.CommandLine.CommandModel
{
    public class CommandSpecification
    {
        public MethodInfo MethodInfo;
        public string Description;
        public string Name;

        readonly Dictionary<string, CommandParameterSpecification> _parametersSpecifications = new Dictionary<string, CommandParameterSpecification>();

        public ReadOnlyDictionary<string, CommandParameterSpecification> ParametersSpecifications => new ReadOnlyDictionary<string,CommandParameterSpecification>(_parametersSpecifications);

        public CommandSpecification(string name,string description,MethodInfo methodInfo, IList<CommandParameterSpecification> commandParameterSpecifications = null)
        {
            Name = name;
            Description = description;
            MethodInfo = methodInfo;
            if (commandParameterSpecifications != null)
                commandParameterSpecifications.ToList().ForEach(x => _parametersSpecifications.Add(x.Name,x));
        }

        public override string ToString()
        {
            var r = $"{Name}";
            var parameters = new SortedList<int, string>();
            foreach (var p in _parametersSpecifications.Values)
                if (p.Index > -1)
                    parameters.Add(p.Index, p.ToString());
            foreach (var p in _parametersSpecifications.Values)
                if (p.Index > -1)
                    parameters.Add(parameters.Keys.Max()+1, p.ToString());
            return r+((parameters.Values.Count==0)?"":(" "+string.Join(' ',parameters.Values)));
        }
    }
}