using DotNetConsoleSdk.Component.CommandLine.CommandModel;
using DotNetConsoleSdk.Component.CommandLine.Parsing;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using static DotNetConsoleSdk.DotNetConsole;
using cons=DotNetConsoleSdk.DotNetConsole;

namespace DotNetConsoleSdk.Component.CommandLine.Commands
{
    public class CommandLineProcessorCommands
    {
        [Command("print help about commands or a specific command")]
        public void Help(
            [Parameter("prints help about command having this name",true)] string commandName = "",
            [Option("short", "set short view", true)] bool shortView = false,
            [Option("v","set verbose view",true)] bool verbose = false
            )
        {
            var cmds = CommandLineProcessor.AllCommands.AsQueryable();
            if (!string.IsNullOrWhiteSpace(commandName))
                cmds = cmds.Where(x => x.Name.Equals(commandName, CommandLineParser.SyntaxMatchingRule));
            if (cmds.Count()>0)
                foreach (var cmd in cmds )
                    PrintCommandHelp(cmd, shortView, verbose, -1);
            else
                Println($"{Red}Command not found: '{commandName}'");
        }

        static void PrintCommandHelp(CommandSpecification com,bool shortView=false,bool verbose=false,int maxcnamelength=-1)
        {
#pragma warning disable IDE0071 // Simplifier l’interpolation
#pragma warning disable IDE0071WithoutSuggestion // Simplifier l’interpolation

            if (maxcnamelength == -1) maxcnamelength = com.Name.Length + TabLength;
            var col = "".PadRight(maxcnamelength, ' ');
            Println($"{com.Name.PadRight(maxcnamelength, ' ')}{com.Description}");
            
            if (com.ParametersCount > 0)
            {
                Println($"{col}{Cyan}syntax: {White}{com.ToColorizedString()}");
                if (!shortView || verbose)
                {
                    var mpl = com.ParametersSpecifications.Values.Select(x => x.Dump(false).Length).Max() + TabLength;
                    Println();
                    foreach (var p in com.ParametersSpecifications.Values)
                        Println($"{col}{Tab}{p.ToColorizedString(false)}{"".PadRight(mpl-p.Dump(false).Length, ' ')}{p.Description}");
                }
            }
            if (verbose)
            {
                Println();
                Println($"{col}{Gray}declaring type: {Darkgray}{com.MethodInfo.DeclaringType.FullName}");
            }
            Println();

#pragma warning restore IDE0071WithoutSuggestion // Simplifier l’interpolation
#pragma warning restore IDE0071 // Simplifier l’interpolation
        }

        [Command("exit the command line processor process")]
        public void Exit()
        {
            cons.Exit();
        }
    }
}
