using DotNetConsoleAppToolkit.Commands.FileSystem;
using DotNetConsoleAppToolkit.Component.CommandLine;
using DotNetConsoleAppToolkit.Component.CommandLine.CommandModel;
using DotNetConsoleAppToolkit.Console;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using static DotNetConsoleAppToolkit.Console.Interaction;
using static DotNetConsoleAppToolkit.DotNetConsole;
using static DotNetConsoleAppToolkit.Lib.FIleReader;
using static DotNetConsoleAppToolkit.Lib.Str;
using sc = System.Console;
using static DotNetConsoleAppToolkit.Lib.FileSystem;
using System.IO;

namespace DotNetConsoleAppToolkit.Commands.TextFile
{
    [Commands("commands related to text files")]
    public class TextFileCommands : CommandsType
    {
        public TextFileCommands(CommandLineProcessor commandLineProcessor) : base(commandLineProcessor) { }

        [Command("file viewer")]
        public void More(
            [Parameter("file or folder path")] WildcardFilePath path,
            [Option("h", "hide line numbers")] bool hideLineNumbers
            )
        {
            if (path.CheckExists())
            {
                var counts = new FindCounts();
                var items = FindItems(path.FullName, path.WildCardFileName ?? "*", true, false, false, true, false, null, false, counts, false, false);
                foreach (var item in items) PrintFile((FilePath)item, hideLineNumbers);
                if (items.Count == 0) Errorln($"more: no such file: {path.OriginalPath}");
                Out.ShowCur();
            }
        }

        [SuppressMessage("Style", "IDE0071:Simplifier l’interpolation", Justification = "<En attente>")]
        [SuppressMessage("Style", "IDE0071WithoutSuggestion:Simplifier l’interpolation", Justification = "<En attente>")]
        void PrintFile(FilePath file, bool hideLineNumbers)
        {
            const int cl = -14;
            string quit = $"{ColorSettings.ParameterName}{$"q|Q",cl}{ColorSettings.Default}quit";
            string help = $"{ColorSettings.ParameterName}{$"h|H",cl}{ColorSettings.Default}print this help";
            string scrollnext = $"{ColorSettings.ParameterName}{$"space",cl}{ColorSettings.Default}display next lines of text, according to current screen size";
            string scrolllinedown = $"{ColorSettings.ParameterName}{$"down arrow",cl}{ColorSettings.Default}scroll one line down";
            string scrolllineup = $"{ColorSettings.ParameterName}{$"up arrow",cl}{ColorSettings.Default}scroll one line up";
            string pagedown = $"{ColorSettings.ParameterName}{$"right arrow",cl}{ColorSettings.Default}jump one page down, according to current screen size";
            string pageup = $"{ColorSettings.ParameterName}{$"left arrow",cl}{ColorSettings.Default}jump one page up, according to current screen size";
            string totop = $"{ColorSettings.ParameterName}{$"t|T",cl}{ColorSettings.Default}jump to the top of the file";
            string toend = $"{ColorSettings.ParameterName}{$"e|E",cl}{ColorSettings.Default}jump to the end of the file";

            var inputMaps = new List<InputMap>
            {
                new InputMap("q",quit),
                new InputMap("h",help),
                new InputMap(" ",scrollnext),
                new InputMap((str,key)=>key.Key==ConsoleKey.DownArrow?InputMap.ExactMatch:InputMap.NoMatch,scrolllinedown),
                new InputMap((str,key)=>key.Key==ConsoleKey.UpArrow?InputMap.ExactMatch:InputMap.NoMatch,scrolllineup),
                new InputMap((str,key)=>key.Key==ConsoleKey.RightArrow?InputMap.ExactMatch:InputMap.NoMatch,pagedown),
                new InputMap((str,key)=>key.Key==ConsoleKey.LeftArrow?InputMap.ExactMatch:InputMap.NoMatch,pageup),
                new InputMap("t",totop),
                new InputMap("e",toend)
            };

            var fileEncoding = file.GetEncoding(Encoding.Default);
            //var lines = fileEncoding == null ? File.ReadAllLines(file.FullName, fileEncoding).ToArray() : File.ReadAllLines(file.FullName).ToArray();
            var (rlines, filePlatform, _) = ReadAllLines(file.FullName);
            var lines = rlines.ToArray();
            var nblines = lines.Length;

            var infos = $"    ({Plur("line", nblines)},encoding={(fileEncoding != null ? fileEncoding.EncodingName : "?")},eol={filePlatform})";
            var n = file.Name.Length + TabLength + infos.Length;
            var sep = "".PadRight(n + 1, '-');
            Out.Println($"{ColorSettings.TitleBar}{sep}");
            Out.Println($"{ColorSettings.TitleBar} {file.Name}{ColorSettings.TitleDarkText}{infos.PadRight(n - file.Name.Length, ' ')}");
            Out.Println($"{ColorSettings.TitleBar}{sep}{ColorSettings.Default}");

            var preambleHeight = 3;
            var linecollength = nblines.ToString().Length;
            var pos = 0;
            bool end = false;
            int y = 0, x = 0;
            var actualWorkArea = DotNetConsole.ActualWorkArea();
            int maxk = actualWorkArea.Bottom - actualWorkArea.Top + 1;
            int k = maxk;
            bool endReached = false;
            bool topReached = true;
            bool skipPrint = false;
            bool scroll1down = false;
            bool forcePrintInputBar = false;
            int decpos = 0;

            while (!end)
            {
                var h = k - 1 - preambleHeight;
                var curNbLines = Math.Min(nblines, h);
                var percent = nblines == 0 ? 100 : Math.Ceiling((double)(Math.Min(curNbLines + pos + decpos, nblines)) / (double)nblines * 100d);
                int i = 0;
                if (!skipPrint)
                    lock (ConsoleLock)
                    {
                        Out.HideCur();
                        while (i < curNbLines && pos + decpos + i < nblines)
                        {
                            if (CommandLineProcessor.CancellationTokenSource.IsCancellationRequested) return;
                            var prefix = hideLineNumbers ? "" : (ColorSettings.Dark + "  " + (pos + decpos + i + 1).ToString().PadRight(linecollength, ' ') + "  ");
                            Out.Println(prefix + ColorSettings.Default + lines[pos + decpos + i]);
                            i++;
                        }
                        Out.ShowCur();
                        y = sc.CursorTop;
                        x = sc.CursorLeft;
                        endReached = pos + i >= nblines;
                        topReached = pos == 0;
                    }
                var inputText = $"--more--({percent}%)";

                var action = end ? quit : InputBar(inputText, inputMaps);
                end = (string)action == quit;

                var oldpos = pos;

                if ((string)action == scrollnext) { k = maxk; pos += k - 1 - preambleHeight; }
                if ((string)action == scrolllinedown && !endReached)
                {
                    if (!scroll1down)
                    {
                        scroll1down = true;
                        decpos = k - 1 - preambleHeight - 1;
                    }
                    pos++;
                    k = 2;
                }
                else
                {
                    scroll1down = false;
                    decpos = 0;
                }

                if ((string)action == totop) { k = maxk; pos = 0; if (pos != oldpos) Out.ClearScreen(); }
                if ((string)action == toend) { k = maxk; pos = Math.Max(0, nblines - maxk + 1); if (pos != oldpos) Out.ClearScreen(); }

                if ((string)action == scrolllineup && !topReached)
                {
                    Out.ClearScreen(); k = maxk; pos = Math.Max(0, pos - 1);
                }
                if ((string)action == pagedown && !endReached) { Out.ClearScreen(); k = maxk; pos += k - 1 - preambleHeight; }
                if ((string)action == pageup && !topReached) { Out.ClearScreen(); k = maxk; pos = Math.Max(0, pos - k + 1); }

                if ((string)action == help)
                {
                    var sepw = inputMaps.Select(x => ((string)x.Code).Length).Max();
                    var hsep = "".PadRight(sepw + 10, '-');
                    Out.Println(Br + hsep + Br);
                    inputMaps.ForEach(x => Out.Println((string)x.Code + Br));
                    Out.Println(hsep);
                    forcePrintInputBar = true;
                }

                preambleHeight = 0;
                skipPrint = oldpos == pos;

                lock (ConsoleLock)
                {
                    sc.CursorLeft = x;
                    if (forcePrintInputBar || !skipPrint || end)
                    {
                        Out.Print("".PadLeft(inputText.Length, ' '));
                        sc.CursorLeft = x;
                        forcePrintInputBar = false;
                    }
                }
            }
        }

        [Command("check integrity of one or several text files","output a message for each corrupted file.\nThese command will declares a text file to be not integre as soon that it detects than the ratio of non printable caracters (excepted CR,LF) is geater than a fixed amount when reading the file")]
        public void CheckIntegrity(
            [Parameter( "path of a file to be checked or path from where find files to to be checked")] FileSystemPath fileOrDir,
            [Option("p", "select names that matches the pattern", true, true)] string pattern,
            [Option("i", "if set and p is set, perform a non case sensisitive search")] bool ignoreCase,
            [Option("a", "print file system attributes")] bool printAttr,
            [Option("t", "search in top directory only")] bool top,
            [Option("r", "acceptable ratio of non printable characters",true,true)] double ratio = 30,
            [Option("s", "minimum size of analysed part of the text",true,true)] int minSeqLength = 1024
            )
        {
            if (fileOrDir.CheckExists())
            {
                if (fileOrDir.IsFile)
                {
                    CheckIntegrity(new FilePath(fileOrDir.FullName),ratio,printAttr, minSeqLength);
                }
                else
                    Errorln("find in checkintegrity command not yet implemented");
            }
        }

        bool CheckIntegrity(
            FilePath filePath,
            double maxRatio,
            bool printAttr,
            int minSeqLength
            )
        {
            var str = File.ReadAllText(filePath.FullName);
            var arr = str.ToCharArray();
            var r = true;
            double nonPrintableCount = 0;
            double rt = 0;
            var cti = arr.Length-1;

            for (int i=0;i<arr.Length;i++)
            {
                if (arr[i]!=10 && arr[i]!=13 && ( arr[i]<32 || arr[i]>255) ) nonPrintableCount++;
                rt = nonPrintableCount / (i+1) * 100d;
                if (rt>maxRatio && i>minSeqLength)
                {
                    cti = i;
                    r = false;
                    break;
                }
            }
            r &= rt <= maxRatio;
            if (!r)
            {
                filePath.Print(printAttr, false, "", $"{Red} seems corrupted from index {cti}: bad chars ratio={rt}%");
                Out.LineBreak();
            }
            return r;
        }
    }
}
