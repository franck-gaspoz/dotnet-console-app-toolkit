using DotNetConsoleSdk.Component.CommandLine.CommandModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace DotNetConsoleSdk.Component.CommandLine.Parsing
{
    public class Syntax
    {
        public readonly string Patterns;

        CommandSpecification _comSpec;

        public Syntax(CommandSpecification comSpec)
        {
            _comSpec = comSpec;
        }

        public override string ToString()
        {
            return _comSpec.ToString();
        }
    }
}
