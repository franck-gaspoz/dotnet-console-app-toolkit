using System;

namespace DotNetConsoleAppToolkit.Component.CommandLine.CommandModel
{
    [AttributeUsage(AttributeTargets.Method,AllowMultiple =false,Inherited =false)]
    public class CommandAttribute : Attribute
    {
        public readonly string Description;
        public readonly string Name;

        public CommandAttribute(string description,string name=null)
        {
            Description = description;
            Name = name;
        }
    }
}
