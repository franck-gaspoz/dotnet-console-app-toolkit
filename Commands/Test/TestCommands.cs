using DotNetConsoleAppToolkit.Commands.FileSystem;
using DotNetConsoleAppToolkit.Component.CommandLine;
using DotNetConsoleAppToolkit.Component.CommandLine.CommandModel;
using DotNetConsoleAppToolkit.Console;
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

        [Command("show current colors support and current colors map using ANSI escape codes")]
        public void ANSIColorTest()
        {
            var colw = 8;
            var totw = colw * 8 + 3 + 10;
            var hsep = "".PadLeft(totw, '-');
            var esc = (char)27;
            Println("Background | Foreground colors");
            Println(hsep);
            for (int j=0;j<=7;j++)
            {
                var str1 = $" ESC[4{j}m   | {esc}[4{j}m";
                var str2 = str1;
                for (int i=0;i<=7;i++)
                {
                    str1 += $"{esc}[3{i}m [3{i}m   ";
                    str2 += $"{esc}[1;3{i}m [1;3{i}m ";
                }
                Println(str1+ColorSettings.Default);
                Println(str2+ColorSettings.Default);
                Println(hsep);
            }
        }
    }
}
