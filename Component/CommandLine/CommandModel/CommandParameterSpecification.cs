using System.Reflection;
using static DotNetConsoleSdk.DotNetConsole;

namespace DotNetConsoleSdk.Component.CommandLine.CommandModel
{
    public class CommandParameterSpecification
    {
        public readonly string ParameterName;
        public readonly ParameterInfo ParameterInfo;
        public readonly bool IsOptional = false;
        public readonly int Index = -1;
        public readonly string Description;
        public readonly string OptionName = null;
        public readonly string Name = null;
        public readonly object DefaultValue = null;
        public readonly bool HasDefaultValue = false;

        public CommandParameterSpecification(
            string parameterName,
            string description, 
            bool isOptional, 
            int index, 
            string name,
            string optionName,
            bool hasDefaultValue,
            object defaultValue,
            ParameterInfo parameterInfo)
        {
            ParameterName = parameterName;
            ParameterInfo = parameterInfo;
            Description = description;
            IsOptional = isOptional;
            Index = index;
            Name = name;
            OptionName = optionName;
            HasDefaultValue = hasDefaultValue;
            DefaultValue = defaultValue;
        }

        public override string ToString()
        {
            var r = $"{Name}";
            if (OptionName != null) r = $"-{OptionName}";
            if (IsOptional) r = $"({r})?";
            if (HasDefaultValue) r += $"={($"{DefaultValue}" ?? $"null")}";
            return r;
        }

        public string ToColorizedString()
        {
            var f = White;
            var r = $"{Yellow}{Name}{f}";
            if (OptionName != null) r = $"{Cyan}-{Green}{OptionName}{f}";
            if (IsOptional) r = $"{Cyan}({r}{Cyan})?{f}";
            if (HasDefaultValue) r += $"{Cyan}={($"{Darkyellow}{DefaultValue}{f}" ?? $"{Darkyellow}null{f}")}";
            return r;
        }
    }
}
