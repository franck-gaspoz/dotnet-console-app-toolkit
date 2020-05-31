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
        public readonly bool AutoName = false;

        /// <summary>
        /// fixed at position=index, non optional or optinal if alone
        /// </summary>
        /// <param name="index"></param>
        /// <param name="description"></param>
        public ParameterAttribute(int index,string description,bool isOptional)
        {
            if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));
            Index = index;
            Description = description ?? throw new ArgumentNullException(nameof(description));
            IsOptional = isOptional;
        }

        /// <summary>
        /// named, can be optional, must have a value if not optional, not fixed
        /// </summary>
        /// <param name="optionName">name of the 'option' parameter (eg. --name,-name,/name)</param>
        /// <param name="description"></param>
        /// <param name="isOptional"></param>
        public ParameterAttribute(string optionName,string description, bool isOptional = false)
        {
            OptionName = optionName ?? throw new ArgumentNullException(nameof(optionName));
            Description = description ?? throw new ArgumentNullException(nameof(description));
            IsOptional = isOptional;
        }

        /// <summary>
        /// named as method parameter, can be optional, must have a value if not optional, not fixed
        /// </summary>
        /// <param name="description"></param>
        /// <param name="isOptional"></param>
        public ParameterAttribute(string description,bool isOptional=false)
        {
            Description = description ?? throw new ArgumentNullException(nameof(description));
            IsOptional = isOptional;
            AutoName = true;
        }
    }
}
