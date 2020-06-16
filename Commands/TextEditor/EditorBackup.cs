using DotNetConsoleAppToolkit.Commands.FileSystem;
using DotNetConsoleAppToolkit.Console;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;

namespace DotNetConsoleAppToolkit.Commands.TextEditor
{
    public class EditorBackup
    {
        public readonly FilePath FilePath;
        public readonly int FirstLine = 0;
        public readonly int CurrentLine = 0;
        public readonly int X = 0;
        public readonly int Y = 0;
        public readonly List<string> Text;
        public readonly List<List<LineSplit>> LinesSplits;
        public readonly Encoding FileEncoding;
        public readonly OSPlatform? FileEOL;
        public readonly Point BeginOfLineCurPos;
        public readonly int LastVisibleLineIndex;
        public readonly int SplitedLastVisibleLineIndex;

        public EditorBackup(
            FilePath filePath,
            int firstLine,
            int currentLine,
            int x,
            int y,
            List<string> text,
            List<List<LineSplit>> linesSplits,
            Encoding fileEncoding,
            OSPlatform? fileEOL,
            Point beginOfLineCurPos,
            int lastVisibleLineIndex,
            int splitedLastVisibleLineIndex)
        {
            FilePath = filePath;
            FirstLine = firstLine;
            CurrentLine = currentLine;
            X = x;
            Y = y;
            Text = new List<string>();
            Text.AddRange(text);
            LinesSplits = new List<List<LineSplit>>();
            LinesSplits.AddRange(linesSplits);
            FileEncoding = fileEncoding;
            FileEOL = fileEOL;
            BeginOfLineCurPos = beginOfLineCurPos;
            LastVisibleLineIndex = lastVisibleLineIndex;
            SplitedLastVisibleLineIndex = splitedLastVisibleLineIndex;
        }
    }
}
