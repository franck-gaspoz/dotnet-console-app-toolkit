using System;
using System.Collections.Generic;
using System.Text;

namespace DotNetConsoleSdk.Component.CommandLine.CommandModel
{
    [AttributeUsage(AttributeTargets.Method,AllowMultiple =false,Inherited =false)]
    public class CommandAttribute : Attribute
    {
        public readonly string Description;

        public CommandAttribute(string description)
        {
            Description = description;
        }
    }
}
