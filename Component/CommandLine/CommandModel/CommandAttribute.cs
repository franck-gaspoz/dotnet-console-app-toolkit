using System;

namespace DotNetConsoleAppToolkit.Component.CommandLine.CommandModel
{
    [AttributeUsage(AttributeTargets.Method,AllowMultiple =false,Inherited =false)]
    public class CommandAttribute : Attribute
    {
        public readonly string Description;
        public readonly string LongDescription;
        public readonly string Name;

        public CommandAttribute(string description,string longDescription=null,string name=null)
        {
            Description = description;
            LongDescription = longDescription;
            Name = name;
        }
    }
}
