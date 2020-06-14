using DotNetConsoleAppToolkit.Commands.FileSystem;
using DotNetConsoleAppToolkit.Component.CommandLine;
using DotNetConsoleAppToolkit.Component.CommandLine.CommandModel;
using DotNetConsoleAppToolkit.Lib;
using System.Collections.Generic;
using System.Reflection.Metadata;
using static DotNetConsoleAppToolkit.DotNetConsole;
using sc = System.Console;
using static DotNetConsoleAppToolkit.Lib.Str;
using System.Text;
using System.Runtime.InteropServices;
using System.Linq;
using DotNetConsoleAppToolkit.Console;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Drawing;

namespace DotNetConsoleAppToolkit.Commands
{
    [Commands("Text Editor")]
    public class TextEditor : CommandsType
    {
        public TextEditor(CommandLineProcessor commandLineProcessor) : base(commandLineProcessor) { }

        int _width;
        int _height;
        FilePath _filePath;
        int _firstLine = 0;
        int _currentLine = 0;
        Point _bkCursorPos;
        int _X = 0;
        int _Y = 0;
        int _barY;
        int _barHeight = 2;
        ConsoleKeyInfo _lastKeyInfo;
        List<string> _text;
        Encoding _fileEncoding;
        OSPlatform? _oSPlatform;
        string _plateform
        {
            get
            {
                return _oSPlatform == null ? "?" : _oSPlatform.Value.ToString();
            }
        }

        [Command("text editor")]
        public void Edit(
            [Parameter("path of an existing or of a new file. the path directory must exists",true)] FilePath filePath
            )
        {
            if (filePath==null || filePath.CheckPathExists())
            {
                _filePath = filePath;
                LoadFile(filePath);
                ShowEditor();
            }
        }

        void ShowEditor()
        {
            ClearScreen();
            ShowCur();
            _width = sc.WindowWidth;
            _height = sc.WindowHeight;
            _barY = _height - _barHeight;
            SetCursorHome();
            ShowFile();
            ShowInfoBar();
            SetCursorHome();
            WaitKeyboard();
        }

        void WaitKeyboard()
        {
            var end = false;
            while (!end)
            {
                var c = sc.ReadKey(true);
                _lastKeyInfo = c;

                var printable = false;
                switch (c.Key)
                {
                    default:
                        printable = true;
                        break;
                }

                if (printable)
                {

                }

                BackupCursorPos();
                ShowInfoBar();
                RestoreCursorPos();
            }
        }

        void BackupCursorPos() => _bkCursorPos = CursorPos;
        void RestoreCursorPos() => SetCursorPos(_bkCursorPos);

        void SetCursorHome()
        {
            _X = 0;
            _Y = 0;
            CursorHome();
        }

        void LoadFile(FilePath filePath)
        {
            if (filePath == null) return;
            _fileEncoding = filePath.GetEncoding(Encoding.Default);
            var (lines, platform) = FIleReader.ReadAllLines(filePath.FullName);
            _text = lines.ToList();
            _oSPlatform = platform;
        }

        void ShowFile()
        {
            int index=_firstLine;
            int y = 0;
            while (y<_height-_barHeight && index<_text.Count)
            {
                var pos = CursorPos;
                PrintLine(index++);
                y = CursorTop;
            }
        }

        void PrintLine(int index)
        {
            Println(_text[index]);
        }

        void ShowInfoBar()
        {
            SetCursorPos(0, _barY);
            Print($"{Invon}{GetFileInfo()}{Clright}{Tdoff}");
            SetCursorPos(0, _barY+1);
            Print($"{Invon}{GetCmdsInfo()}{Clright}{Tdoff}");
            SetCursorPos(_width - 24, _barY + 1);
            Print($"{Invon}{GetLastKeyInfo()} {GetPositionInfo()} {GetCursorInfo()}{Clright}{Tdoff}");
        }

        string GetLastKeyInfo() => _lastKeyInfo.Key+$"({_lastKeyInfo.KeyChar})";
        string GetPositionInfo() => "line "+_currentLine+"";
        string GetCursorInfo() => $"{_X},{_Y}{Clright}";
        string GetFileInfo()
        {
            return (_filePath == null) ? $"no file" : $"{_filePath.Name} | {Plur("line", _text.Count)} | size={HumanFormatOfSize(_filePath.FileInfo.Length,2)} | enc={_fileEncoding.EncodingName} | plateform={_plateform}";
        }

        string GetCmdsInfo()
        {
            static string Opt(string shortCut) => $"{Bwhite}{Black}{shortCut}{ColorSettings.Default}";
            return $"{Opt("^l")} Load | {Opt("^s")} Save | {Opt("^t")} Top | {Opt("^b")} Bottom | {Opt("^x")} Quit | ";
        }
    }
}
