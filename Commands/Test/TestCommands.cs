using DotNetConsoleAppToolkit.Commands.FileSystem;
using DotNetConsoleAppToolkit.Component.CommandLine;
using DotNetConsoleAppToolkit.Component.CommandLine.CommandModel;
using DotNetConsoleAppToolkit.Console;
using System;
using System.IO;
using System.Runtime.CompilerServices;
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
            // 3 bits colors (standard)
            var colw = 8;
            var totw = colw * 8 + 3 + 10;
            var hsep = "".PadLeft(totw, '-');
            var esc = (char)27;
            string r;
            int x2 = 0;

            Println("3 bits (8 color mode)");
            Println();
            Println("Background | Foreground colors");
            Println(hsep);
            for (int j = 0; j <= 7; j++)
            {
                var str1 = $" ESC[4{j}m   | {esc}[4{j}m";
                var str2 = str1;
                for (int i = 0; i <= 7; i++)
                {
                    str1 += $"{esc}[3{i}m [3{i}m   ";
                    str2 += $"{esc}[1;3{i}m [1;3{i}m ";
                }
                Println(str1 + ColorSettings.Default);
                Println(str2 + ColorSettings.Default);
                Println(hsep);
            }
            Println(ColorSettings.Default + "");

            // 8 bits colors
            Println("8 bits (256 color mode)");
            Println();
            Println("216 colors: 16 + 36 × r + 6 × g + b (0 <= r, g, b <= 5)(br)");
            int n = 16;
            for (int y = 0; y < 6; y++)
            {
                r = White;
                for (int x = 16; x <= 51; x++)
                {
                    if (x == 34)
                        r += Black;
                    r += $"{esc}[48;5;{n}m" + ((n + "").PadLeft(4, ' '));
                    n++;
                    x2++;
                    if (x2 >= 6) { r += Br; x2 = 0; }
                }
                Print(r);
            }

            Println(ColorSettings.Default + "");
            Println("grayscale colors (24 colors) : 232 + l (0 <= l <= 24)(br)");
            r = White;
            x2 = 0;
            for (int x = 232; x <= 255; x++)
            {
                if (x == 244)
                    r += Black;
                r += $"{esc}[48;5;{x}m" + ((x + "").PadLeft(4, ' '));
                x2++;
                if (x2 >= 6) { r += Br; x2 = 0; }
            }
            Print(r);

            Println(ColorSettings.Default + "");
            Println("24 bits (16777216 colors): 0 <= r,g,b <= 255 (br) ");

            string cl(int r, int v, int b) =>
                esc + "[48;2;" + r + ";" + v + ";" + b + "m ";

            var stp = 4;
            r = "";
            int cr, cb = 0, cv = 0;
            for (cr = 0; cr < 255; cr += stp)
                r += cl(cr, cv, cb);
            Println(r);

            r = "";
            cr = 0;
            for (cv = 0; cv < 255; cv += stp)
                r += cl(cr, cv, cb);
            Println(r);

            cv = 0;
            r = "";
            for (cb = 0; cb < 255; cb += stp)
                r += cl(cr, cv, cb);
            Println(r);

            r = "";
            for (cb = 0; cb < 255; cb += stp)
                r += cl(cb, cb, 0);
            Println(r);

            r = "";
            for (cb = 0; cb < 255; cb += stp)
                r += cl(cb, 0, cb);
            Println(r);

            r = "";
            for (cb = 0; cb < 255; cb += stp)
                r += cl(0, cb, cb);
            Println(r);

            r = "";
            for (cb = 0; cb < 255; cb += stp)
                r += cl(cb, cb, cb);
            Println(r);

        }
    }
}
