using DotNetConsoleAppToolkit.Component.CommandLine.CommandModel;
using static DotNetConsoleAppToolkit.DotNetConsole;
using sc = System.Console;

namespace DotNetConsoleAppToolkit.Component.CommandLine.Commands
{
    [Commands("tests commands")]
    public class TestCommands : CommandsType
    {
        public TestCommands(CommandLineProcessor commandLineProcessor) : base(commandLineProcessor) { }

        [Command("print cursor info")]
        public void CursorInfo() => Println($"crx={sc.CursorLeft} cry={sc.CursorTop}");
    }
}
