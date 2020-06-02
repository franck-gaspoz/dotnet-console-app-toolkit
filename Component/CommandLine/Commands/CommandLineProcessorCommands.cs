using DotNetConsoleSdk.Component.CommandLine.CommandModel;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using static DotNetConsoleSdk.DotNetConsole;
using cons=DotNetConsoleSdk.DotNetConsole;

namespace DotNetConsoleSdk.Component.CommandLine.Commands
{
    public class CommandLineProcessorCommands
    {
        [Command("print help about commands")]
        public void Help(
            [Option("short","set short view",true)] bool shortView=false
            )
        {
            var coms = CommandLineProcessor.AllCommands;
            var maxcnamelength = coms.Select(x => x.Name.Length).Max()+TabLength;
            foreach (var com in coms)
                PrintCommandHelp(com, shortView, maxcnamelength);
        }

        [Command("print help about a command")]
        public void Help(
            [Parameter("name of the command")] string commandName
            )
        {
            var cmd = CommandLineProcessor.AllCommands.Where(x => x.Name.Equals(commandName, DotNetConsoleSdk.Component.CommandLine.Parsing.CommandLineParser.SyntaxMatchingRule)).FirstOrDefault();
            if (cmd != null)
                PrintCommandHelp(cmd, false, -1);
            else
                Println($"{Red}Unknown command '{commandName}'");
        }

        static void PrintCommandHelp(CommandSpecification com,bool shortView=false,int maxcnamelength=-1)
        {
#pragma warning disable IDE0071 // Simplifier l’interpolation
#pragma warning disable IDE0071WithoutSuggestion // Simplifier l’interpolation

            if (maxcnamelength == -1) maxcnamelength = com.Name.Length + TabLength;
            var col = "".PadRight(maxcnamelength, ' ');
            Println($"{com.Name.PadRight(maxcnamelength, ' ')}{com.Description}");
            
            if (com.ParametersCount > 0)
            {
                Println($"{col}{Cyan}syntax: {White}{com.ToColorizedString()}");
                if (!shortView)
                {
                    var mpl = com.ParametersSpecifications.Values.Select(x => x.Dump(false).Length).Max() + TabLength;
                    Println();
                    foreach (var p in com.ParametersSpecifications.Values)
                        Println($"{col}{Tab}{p.ToColorizedString(false)}{"".PadRight(mpl-p.Dump(false).Length, ' ')}{p.Description}");
                }
            }
            if (!shortView)
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
