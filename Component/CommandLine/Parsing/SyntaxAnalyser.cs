using DotNetConsoleSdk.Component.CommandLine.CommandModel;
using System;
using System.Collections.Generic;
using static DotNetConsoleSdk.DotNetConsole;

namespace DotNetConsoleSdk.Component.CommandLine.Parsing
{
    public class SyntaxAnalyser
    {
        readonly Dictionary<string, List<Syntax>> _syntaxes
            = new Dictionary<string, List<Syntax>>();

        public void Add(CommandSpecification comSpec)
        {
            //Println(comSpec.ToString());
            if (_syntaxes.TryGetValue(comSpec.Name, out var lst))
                lst.Add(new Syntax(comSpec));
            else
                _syntaxes.Add(comSpec.Name, new List<Syntax> { new Syntax(comSpec) });
        }

        public List<Syntax> FindSyntaxesFromToken(
            string token,
            bool partialTokenMatch=false,
            StringComparison comparisonType = StringComparison.CurrentCulture
            )
        {
            var r = new List<Syntax>();
            foreach (var ctoken in _syntaxes.Keys)
                if ((partialTokenMatch && ctoken.StartsWith(token, comparisonType)) || ctoken.Equals(token, comparisonType))
                    r.AddRange(_syntaxes[ctoken]);
            return r;
        }
    }
}
