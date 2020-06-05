using DotNetConsoleSdk.Component.CommandLine.CommandModel;
using DotNetConsoleSdk.Component.CommandLine.Parsing;
using System;
using System.Linq;
using System.Reflection.Metadata;
using static DotNetConsoleSdk.DotNetConsole;
using static DotNetConsoleSdk.Lib.Str;
using cons = DotNetConsoleSdk.DotNetConsole;

namespace DotNetConsoleSdk.Component.CommandLine.Commands
{
    [Commands]
    public class CommandLineProcessorCommands
    {
        [Command("print help about all commands or a specific command")]
        public void Help(
            [Option("short", "set short view", true)] bool shortView,
            [Option("v","set verbose view",true)] bool verbose,
            [Option("list","list all commands names and their description")] bool list,
            [Parameter("prints help for this command name", true)] string commandName = ""
            )
        {
            var cmds = CommandLineProcessor.AllCommands.AsQueryable();
            if (!string.IsNullOrWhiteSpace(commandName))
                cmds = cmds.Where(x => x.Name.Equals(commandName, CommandLineParser.SyntaxMatchingRule));

            if (cmds.Count() > 0)
            {
                var ncmds = cmds.ToList();
                ncmds.Sort(new Comparison<CommandSpecification>((x, y) => x.Name.CompareTo(y.Name)));
                cmds = ncmds.AsQueryable();
                var maxcmdlength = cmds.Select(x => x.Name.Length).Max() + 1;
                var maxcmdtypelength = cmds.Select(x => x.DeclaringTypeShortName.Length).Max() + 1;
                int n = 0;
                foreach (var cmd in cmds)
                {
                    if (!list && n > 0) Println();
                    PrintCommandHelp(cmd, shortView, verbose,list,maxcmdlength, maxcmdtypelength, cmds.Count()==1);
                    n++;
                }
            }
            else
                Errorln($"Command not found: '{commandName}'");
        }

        static void PrintCommandHelp(CommandSpecification com, bool shortView = false, bool verbose = false, bool list = false, int maxcnamelength=-1, int maxcmdtypelength=-1, bool singleout=false)
        {
#pragma warning disable IDE0071 // Simplifier l’interpolation
#pragma warning disable IDE0071WithoutSuggestion // Simplifier l’interpolation

            if (maxcnamelength == -1) maxcnamelength = com.Name.Length + 1;
            if (maxcmdtypelength == -1) maxcmdtypelength = com.DeclaringTypeShortName.Length + 1;       
            var col = singleout? "": "".PadRight(maxcnamelength, ' ');
            var f = GetCmd(KeyWords.f + "", DefaultForeground.ToString().ToLower());
            if (list)
                Println($"{com.Name.PadRight(maxcnamelength, ' ')}{Darkgray}{com.DeclaringTypeShortName.PadRight(maxcmdtypelength, ' ')}{f}{com.Description}");
            else
            {
                if (singleout)
                    Println(com.Description);
                else
                    Println($"{com.Name.PadRight(maxcnamelength, ' ')}{com.Description}");
            }

            if (!list)
            {
                if (com.ParametersCount > 0)
                {
                    Println($"{col}{Cyan}syntax: {f}{com.ToColorizedString()}");
                    Println($"{col}{Cyan}module: {Darkgray}{com.DeclaringTypeShortName}");
                    if (!shortView || verbose)
                    {
                        var mpl = com.ParametersSpecifications.Values.Select(x => x.Dump(false).Length).Max() + TabLength;
                        Println();
                        foreach (var p in com.ParametersSpecifications.Values)
                        {
                            var ptype = (!p.IsOption && p.HasValue) ? $". of type: {Darkyellow}{p.ParameterInfo.ParameterType.Name}{f}" : "";
                            var pdef = (!p.IsOption && p.HasValue) ? ($". default value: {Darkyellow}{DumpAsText(p.DefaultValue)}{f}") : "";
                            Println($"{col}{Tab}{p.ToColorizedString(false)}{"".PadRight(mpl - p.Dump(false).Length, ' ')}{p.Description}{ptype}{pdef}");
                        }
                    }
                }
                if (verbose)
                {
                    Println();
                    Println($"{col}{Gray}declaring type: {Darkgray}{com.MethodInfo.DeclaringType.FullName}");
                }
            }

#pragma warning restore IDE0071WithoutSuggestion // Simplifier l’interpolation
#pragma warning restore IDE0071 // Simplifier l’interpolation
        }

        [Command("exit the shell")]
        public void Exit()
        {
            cons.Exit();
        }
    }
}
