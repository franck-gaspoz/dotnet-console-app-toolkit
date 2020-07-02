using DotNetConsoleAppToolkit.Commands.FileSystem;
using DotNetConsoleAppToolkit.Component.CommandLine;
using DotNetConsoleAppToolkit.Component.CommandLine.CommandModel;
using DotNetConsoleAppToolkit.Console;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using static DotNetConsoleAppToolkit.Console.ANSI;
using static DotNetConsoleAppToolkit.DotNetConsole;
using static DotNetConsoleAppToolkit.Lib.FIleReader;
using sc = System.Console;

namespace DotNetConsoleAppToolkit.Commands.Test
{
    [Commands("tests commands")]
    public class TestCommands : ICommandsDeclaringType
    {
        [Command("print cursor info")]
        public CommandResult<Point> CursorInfo(CommandEvaluationContext context)
        {
            int x = sc.CursorLeft, y = sc.CursorTop;
            context.Out.Println($"crx={x} cry={y}");
            return new CommandResult<Point>( new Point(x, y));
        }

        [Command("check end of line symbols of a file")]
        public CommandResult<List<string>> Fileeol(
            CommandEvaluationContext context,
            [Parameter("file path")] FilePath file)
        {
            var r = new List<string>();
            if (file.CheckExists())
            {
                var (_, eolCounts, _) = GetEOLCounts(File.ReadAllText(file.FullName));
                foreach (var eol in eolCounts)
                {
                    var s = $"{eol.eol}={eol.count}";
                    r.Add(s);
                    context.Out.Println(s);
                }
                return new CommandResult<List<string>>( r);
            }
            else return new CommandResult<List<string>>( r, ReturnCode.Error);
        }

        [Command("show current colors support and current colors map using ANSI escape codes")]
        public CommandVoidResult ANSIColorTest(
            CommandEvaluationContext context
            )
        {
            // 3 bits colors (standard)
            var colw = 8;
            var totw = colw * 8 + 3 + 10;
            var hsep = "".PadLeft(totw, '-');
            var esc = (char)27;
            string r;
            int x2 = 0;

            context.Out.Println("3 bits (8 color mode)");
            context.Out.Println();
            context.Out.Println("Background | Foreground colors");
            context.Out.Println(hsep);
            for (int j = 0; j <= 7; j++)
            {
                var str1 = $" ESC[4{j}m   | {esc}[4{j}m";
                var str2 = $" ESC[10{j}m  | {esc}[10{j}m";
                for (int i = 0; i <= 7; i++)
                {
                    str1 += Set3BitsColors(i, j | 0b1000) + $" [9{i}m   ";
                    str2 += Set3BitsColors(i | 0b1000, j) + $" [3{i}m   ";
                }

                context.Out.Println(str1 + ColorSettings.Default);
                context.Out.Println(str2 + ColorSettings.Default);
                context.Out.Println(hsep);
            }
            context.Out.Println(ColorSettings.Default + "");

            // 8 bits colors
            context.Out.Println("8 bits (256 color mode)");
            context.Out.Println();
            context.Out.Println("216 colors: 16 + 36 × r + 6 × g + b (0 <= r, g, b <= 5)(br)");
            int n = 16;
            for (int y = 0; y < 6; y++)
            {
                r = "";
                for (int x = 16; x <= 51; x++)
                {
                    if (x >= 34)
                        r += Black;
                    else
                        r += White;
                    r += $"{esc}[48;5;{n}m" + ((n + "").PadLeft(4, ' '));
                    n++;
                    x2++;
                    if (x2 >= 6) { r += Br; x2 = 0; }
                }
                context.Out.Print(r);
            }

            context.Out.Println(ColorSettings.Default + "");
            context.Out.Println("grayscale colors (24 colors) : 232 + l (0 <= l <= 24)(br)");
            r = White;
            x2 = 0;
            for (int x = 232; x <= 255; x++)
            {
                if (x >= 244)
                    r += Black;
                r += $"{esc}[48;5;{x}m" + ((x + "").PadLeft(4, ' '));
                x2++;
                if (x2 >= 6) { r += Out.LNBRK; x2 = 0; }
            }
            context.Out.Print(r);

            context.Out.Println(ColorSettings.Default + "");
            context.Out.Println("24 bits (16777216 colors): 0 <= r,g,b <= 255 (br) ");

            string cl(int r, int v, int b) =>
                esc + "[48;2;" + r + ";" + v + ";" + b + "m ";

            var stp = 4;
            r = "";
            int cr, cb = 0, cv = 0;
            for (cr = 0; cr < 255; cr += stp)
                r += cl(cr, cv, cb);
            context.Out.Println(r);

            r = "";
            cr = 0;
            for (cv = 0; cv < 255; cv += stp)
                r += cl(cr, cv, cb);
            context.Out.Println(r);

            cv = 0;
            r = "";
            for (cb = 0; cb < 255; cb += stp)
                r += cl(cr, cv, cb);
            context.Out.Println(r);

            r = "";
            for (cb = 0; cb < 255; cb += stp)
                r += cl(cb, cb, 0);
            context.Out.Println(r);

            r = "";
            for (cb = 0; cb < 255; cb += stp)
                r += cl(cb, 0, cb);
            context.Out.Println(r);

            r = "";
            for (cb = 0; cb < 255; cb += stp)
                r += cl(0, cb, cb);
            context.Out.Println(r);

            r = "";
            for (cb = 0; cb < 255; cb += stp)
                r += cl(cb, cb, cb);
            context.Out.Println(r);

            return new CommandVoidResult();
        }
    }
}
