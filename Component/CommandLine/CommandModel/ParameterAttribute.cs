using System;

namespace DotNetConsoleSdk.Component.CommandLine.CommandModel
{
    [AttributeUsage(AttributeTargets.Parameter,AllowMultiple =false,Inherited =false)]
    public class ParameterAttribute : Attribute
    {
        public readonly bool IsOptional = false;
        public readonly int Index = -1;
        public readonly string Description;
        public readonly string OptionName = null;

        public ParameterAttribute(int index,string description,bool isOptional=false)
        {
            if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));
            Index = index;
            Description = description ?? throw new ArgumentNullException(nameof(description));
            IsOptional = isOptional;
        }

        public ParameterAttribute(string optionName,string description, bool isOptional = false)
        {
            OptionName = optionName ?? throw new ArgumentNullException(nameof(optionName));
            Description = description ?? throw new ArgumentNullException(nameof(description));
            IsOptional = isOptional;
        }

        public ParameterAttribute(string description,bool isOptional=false)
        {
            Description = description ?? throw new ArgumentNullException(nameof(description));
            IsOptional = isOptional;
        }
    }
}
