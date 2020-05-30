using System.Reflection;

namespace DotNetConsoleSdk.Component.CommandLine.CommandModel
{
    public class CommandParameterSpecification
    {
        public readonly string Name;
        public readonly ParameterInfo ParameterInfo;
        public readonly bool IsOptional = false;
        public readonly int Index = -1;
        public readonly string Description;
        public readonly string OptionName = null;
        public readonly object DefaultValue = null;
        public readonly bool HasDefaultValue = false;

        public CommandParameterSpecification(
            string name,
            string description, 
            bool isOptional, 
            int index, 
            string optionName,
            bool hasDefaultValue,
            object defaultValue,
            ParameterInfo parameterInfo)
        {
            Name = name;
            ParameterInfo = parameterInfo;
            Description = description;
            IsOptional = isOptional;
            Index = index;
            OptionName = optionName;
            HasDefaultValue = hasDefaultValue;
            DefaultValue = defaultValue;
        }

        public override string ToString()
        {
            var r = $"Name";
            if (OptionName != null) r = $"-{OptionName} {r}";
            if (IsOptional) r = $"({r})?";
            if (HasDefaultValue) r += $"={(DefaultValue ?? "null")}";
            return r;
        }
    }
}
