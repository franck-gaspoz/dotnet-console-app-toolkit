﻿//#define printDefaultValueInSyntax

using DotNetConsoleSdk.Component.CommandLine.Parsing;
using System.Reflection;
using static DotNetConsoleSdk.DotNetConsole;
using static DotNetConsoleSdk.Lib.Str;

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
        public readonly object DefaultValue = null;
        public readonly bool HasDefaultValue = false;
        public readonly bool HasValue = true;

        public string ActualName => OptionName ?? ParameterName;
        public bool IsOption => OptionName != null;

        public CommandParameterSpecification(
            string parameterName,
            string description, 
            bool isOptional, 
            int index, 
            string optionName,
            bool hasValue,
            bool hasDefaultValue,
            object defaultValue,
            ParameterInfo parameterInfo)
        {
            ParameterName = parameterName;
            ParameterInfo = parameterInfo;
            Description = description;
            IsOptional = isOptional;
            Index = index;
            OptionName = optionName;
            HasValue = hasValue;
            HasDefaultValue = hasDefaultValue;
            DefaultValue = defaultValue;
        }

        public string ParameterValueTypeName => ParameterInfo.ParameterType.Name;

        public int SegmentsCount => IsOption && HasValue ? 2 : 1;

        public override string ToString() => Dump();

        public string Dump(bool grammarSymbolsVisible = true)
        {
            var r = $"{ParameterName}";
            if (IsOption)
            {
                var optVal = (HasValue) ? $" {ParameterValueTypeName}" : "";
                r = $"{ParameterSyntax.OptionPrefix}{OptionName}{optVal}";
            }
            if (IsOptional && grammarSymbolsVisible) r = $"[{r}]";
#if printDefaultValueInSyntax
            if (HasDefaultValue && grammarSymbolsVisible) r += $"{{={($"{DumpAsText(DefaultValue)}")}}}";
#endif
            return r;
        }

        public string ToColorizedString(bool grammarSymbolsVisible=true)
        {
            var f = GetCmd(KeyWords.f + "", DefaultForeground.ToString().ToLower());
            var r = $"{Yellow}{ParameterName}{f}";
            if (IsOption)
            {
                var optVal = (HasValue) ? $" {Darkyellow}{ParameterValueTypeName}" : "";
                r = $"{Darkyellow}{ParameterSyntax.OptionPrefix}{Yellow}{OptionName}{optVal}{f}";
            }
            if (IsOptional && grammarSymbolsVisible) r = $"{Cyan}[{r}{Cyan}]{f}";
#if printDefaultValueInSyntax
            if (HasDefaultValue && grammarSymbolsVisible) r += $"{Cyan}{{={($"{Darkyellow}{DumpAsText(DefaultValue)}{Cyan}}}{f}")}";
#endif
            return r;
        }
    }
}
