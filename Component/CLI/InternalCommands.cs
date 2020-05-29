using ConsoleAppFramework;

namespace DotNetConsoleSdk.Component.CLI
{
    internal class InternalCommands : ConsoleAppBase
    {
        [Command("print")]
        public void Print([Option(0,"expression to be evaluated and printed to the output stream")]string expr)
        {
            DotNetConsole.Print(expr);
        }
    }
}
