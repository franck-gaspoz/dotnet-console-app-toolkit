using DotNetConsoleAppToolkit.Component.CommandLine.CommandModel;
using static DotNetConsoleAppToolkit.DotNetConsole;
using sc = System.Console;
using static DotNetConsoleAppToolkit.Lib.FIleReader;
using DotNetConsoleAppToolkit.Commands.FileSystem;
using System.IO;

namespace DotNetConsoleAppToolkit.Component.CommandLine.Commands
{
    [Commands("tests commands")]
    public class TestCommands : CommandsType
    {
        public TestCommands(CommandLineProcessor commandLineProcessor) : base(commandLineProcessor) { }

        [Command("print cursor info")]
        public void CursorInfo() => Println($"crx={sc.CursorLeft} cry={sc.CursorTop}");

        [Command("check end of line symbols of a file")]
        public void Fileeol(
            [Parameter("file path")] FilePath file)
        {
            if (file.CheckExists())
            {
                var eols = GetEOLCounts(File.ReadAllText(file.FullName));
                foreach (var eol in eols.eolCounts)
                    Println($"{eol.eol}={eol.count}");
            }
        }
    }
}
