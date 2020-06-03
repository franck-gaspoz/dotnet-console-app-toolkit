using DotNetConsoleSdk.Component.CommandLine.CommandModel;
using static DotNetConsoleSdk.DotNetConsole;
using cons = DotNetConsoleSdk.DotNetConsole;

namespace DotNetConsoleSdk.Component.CommandLine.Commands
{
    public class ConsoleCommands
    {
        [Command("write any given expression to the output stream")]
        public void Print(
            [Parameter("expression that will be write to output",true)] string expr = ""
            ) => DotNetConsole.Print(expr);

        [Command("write any given expression to the output stream followed by a line break")]
        public void Println(
            [Parameter("expression that will be write to output", true)] string expr = ""
            ) => DotNetConsole.Println(expr);

        [Command("clear console screen")]
        public void Cls() => Clear();

        [Command("hide cursor")]
        public void HideCursor() => HideCur();

        [Command("show cursor")]
        public void ShowCursor() => ShowCur();
    }
}
