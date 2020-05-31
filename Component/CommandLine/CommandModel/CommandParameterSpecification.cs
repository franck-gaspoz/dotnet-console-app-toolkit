#define coloredToString

using System.Reflection;
using static DotNetConsoleSdk.DotNetConsole;

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
            var f = White;
            var r = $"{Yellow}{Name}{f}";
            if (OptionName != null) r = $"{Cyan}-{Green}{OptionName}{f} {r}";
            if (IsOptional) r = $"{Cyan}({r}{Cyan})?{f}";
            if (HasDefaultValue) r += $"{Cyan}={($"{Darkyellow}{DefaultValue}{f}" ?? $"{Darkyellow}null{f}")}";
#if !coloredToString
            r = GetPrint(r);
#endif
            return r;
        }
    }
}
