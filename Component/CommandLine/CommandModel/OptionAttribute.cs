using System;

namespace DotNetConsoleSdk.Component.CommandLine.CommandModel
{
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    public class OptionAttribute : Attribute
    {
        public readonly bool IsOptional = false;
        public readonly string Description;
        public readonly string OptionName = null;

        public OptionAttribute(string optionName,string description,bool isOptional)
        {
            OptionName = optionName;
            Description = description ?? throw new ArgumentNullException(nameof(description));
            IsOptional = isOptional;
        }
    }
}
