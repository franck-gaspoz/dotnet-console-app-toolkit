using DotNetConsoleSdk.Component.CommandLine.CommandModel;

namespace DotNetConsoleSdk.Component.CommandLine.Commands
{
    public class CommandEngineCommands
    {
        [Command("print help about commands")]
        public void Help()
        {
            foreach ( var kvp in CommandEngine.Commands )
            {

            }
        }

        [Command("print help about a command")]
        public void Help([Parameter("name of the command")]string commandName)
        {

        }

        static string DumpCommandHelp(CommandSpecification cmdspec)
        {
            return null;
        }
    }
}
