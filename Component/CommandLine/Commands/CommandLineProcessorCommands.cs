using DotNetConsoleSdk.Component.CommandLine.CommandModel;
using System.Linq;
using static DotNetConsoleSdk.DotNetConsole;

namespace DotNetConsoleSdk.Component.CommandLine.Commands
{
    public class CommandLineProcessorCommands
    {
        [Command("print help about commands")]
        public void Help([Option("short","set short view",true)]bool shortView=false)
        {
            var coms = CommandLineProcessor.AllCommands;
            var maxcnamelength = coms.Select(x => x.Name.Length).Max()+TabLength;
            foreach (var com in coms)
                PrintCommandHelp(com, shortView, maxcnamelength);
        }

        [Command("print help about a command")]
        public void Help([Parameter("name of the command")]string commandName)
        {

        }

        static void PrintCommandHelp(CommandSpecification com,bool shortView=false,int maxcnamelength=-1)
        {
#pragma warning disable IDE0071 // Simplifier l’interpolation
#pragma warning disable IDE0071WithoutSuggestion // Simplifier l’interpolation

            if (maxcnamelength == -1) maxcnamelength = com.Name.Length;
            var col = "".PadRight(maxcnamelength, ' ');
            Println($"{com.Name.PadRight(maxcnamelength, ' ')}{com.Description}");
            if (!shortView)
            {
                if (com.ParametersCount > 0)
                {
                    Println($"{col}{Cyan}syntax: {White}{com.ToColorizedString()}");
                    var mpl = com.ParametersSpecifications.Values.Select(x => x.ToString().Length).Max() + TabLength;
                    Println();
                    foreach (var kvp in com.ParametersSpecifications)
                        Println($"{col}{Tab}{kvp.Value.ToString().PadRight(mpl, ' ')}{kvp.Value.Description}");
                }
                Println();
                Println($"{col}{Cyan}defined in: {Gray}{com.MethodInfo.DeclaringType.FullName}");
                Println();
            }

#pragma warning restore IDE0071WithoutSuggestion // Simplifier l’interpolation
#pragma warning restore IDE0071 // Simplifier l’interpolation
        }
    }
}
