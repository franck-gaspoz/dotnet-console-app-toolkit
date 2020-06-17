using DotNetConsoleAppToolkit.Commands.FileSystem;
using DotNetConsoleAppToolkit.Component.CommandLine;
using DotNetConsoleAppToolkit.Component.CommandLine.CommandModel;
using System.IO;
using static DotNetConsoleAppToolkit.DotNetConsole;
using static DotNetConsoleAppToolkit.Lib.FIleReader;
using sc = System.Console;

namespace DotNetConsoleAppToolkit.Commands.Test
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
                var (_, eolCounts, _) = GetEOLCounts(File.ReadAllText(file.FullName));
                foreach (var eol in eolCounts)
                    Println($"{eol.eol}={eol.count}");
            }
        }
    }
}
