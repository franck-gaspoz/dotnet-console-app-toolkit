using DotNetConsoleSdk.Component.CommandLine.CommandModel;

namespace DotNetConsoleSdk.Component.CommandLine.Commands
{
    public class StdoutCommands
    {
        [Command("write any given expression to the output stream")]
        public void Print(
            [Parameter(0,"expression that will be write to output",true)]string expr)
        {
            DotNetConsole.Print(expr);
        }

        [Command("write any given expression to the output stream followed by a line break")]
        public void Println(
            [Parameter(0, "expression that will be write to output", true)]string expr)
        {
            DotNetConsole.Println(expr);
        }
    }
}
