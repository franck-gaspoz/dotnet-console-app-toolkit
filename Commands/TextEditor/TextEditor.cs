#define dbg

using DotNetConsoleAppToolkit.Commands.FileSystem;
using DotNetConsoleAppToolkit.Component.CommandLine;
using DotNetConsoleAppToolkit.Component.CommandLine.CommandModel;
using DotNetConsoleAppToolkit.Console;
using DotNetConsoleAppToolkit.Lib;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text;
using static DotNetConsoleAppToolkit.DotNetConsole;
using cons = DotNetConsoleAppToolkit.DotNetConsole;
using static DotNetConsoleAppToolkit.Lib.Str;
using sc = System.Console;

namespace DotNetConsoleAppToolkit.Commands.TextEditor
{
    [Commands("Text Editor")]
    public class TextEditor : CommandsType
    {
        public TextEditor(CommandLineProcessor commandLineProcessor) : base(commandLineProcessor) { }

        #region attributes

        int _width;
        int _height;
        FilePath _filePath;
        int _firstLine = 0;
        int _currentLine = 0;
        Point _bkCursorPos;
        int _X = 0;
        int _Y = 0;
        int _barY;
        int _barHeight;
        ConsoleKeyInfo _lastKeyInfo;
        List<string> _text;
        List<int> _linesHeight;
        Encoding _fileEncoding;
        OSPlatform? _fileEOL;
        string FileEOL
        {
            get
            {
                return _fileEOL == null ? "?" : _fileEOL.Value.ToString();
            }
        }
        bool _cmdInput = false;
        string _statusText;
        ConsoleKey _cmdKey;
        string _cmdKeyStr;
        Point _beginOfLineCurPos = new Point(0, 0);

        #endregion

        [Command("text editor")]
        public void Edit(
            [Parameter("path of an existing or of a new file. the path directory must exists",true)] FilePath filePath
            )
        {
            if (filePath==null || filePath.CheckPathExists())
            {
                _filePath = filePath;
                LoadFile(filePath);
                InitEditor();
                ShowEditor();
            }
        }

        void InitEditor()
        {
            _firstLine = 0;
            _currentLine = 0;
            _X = 0;
            _Y = 0;
            _cmdKey = ConsoleKey.Escape;
            _barHeight = 2;
            _cmdKeyStr = "Esc ";
        }

        void ShowEditor()
        {
            try
            {
                HideCur();
                ClearScreen();
                ShowCur();
                _width = sc.WindowWidth;
                _height = sc.WindowHeight;
                _barY = _height - _barHeight;
                SetCursorHome();
                ShowFile();
                ClearInfoBar();
                ShowInfoBar(false);
                SetCursorHome();
                ShowCur();
                WaitAndProcessKeyPress();
            } catch (Exception ex)
            {
                Errorln(ex+"");
            }
        }

        void WaitAndProcessKeyPress()
        {
            var end = false;
            _beginOfLineCurPos = new Point(0, _Y);
            while (!end)
            {
                var c = sc.ReadKey(true);
                _lastKeyInfo = c;
                var (id, left, top, right, bottom) = cons.ActualWorkArea(false);

                var printable = false;
                bool printOnlyCursorInfo = true;
                switch (c.Key)
                {
                    case ConsoleKey.LeftArrow:
                        lock (ConsoleLock)
                        {
                            var p = CursorPos;
                            if (p.Y == _beginOfLineCurPos.Y)
                            {
                                if (p.X > _beginOfLineCurPos.X)
                                    SetCursorLeft(p.X - 1);                                
                            }
                            else
                            {
                                var x = p.X - 1;
                                if (x < left)
                                    SetCursorPosConstraintedInWorkArea(right - 1, p.Y - 1,true,true,false);
                                else
                                    SetCursorLeft(x);
                            }
                            _X = CursorLeft;
                            _Y = CursorTop;
                        }
                        break;

                    case ConsoleKey.RightArrow:
                        lock (ConsoleLock)
                        {
                            var line = _text[_currentLine];
                            var index = GetIndexInWorkAreaConstraintedString(line, _beginOfLineCurPos, CursorPos,true,false);
                            if (index < line.Length-1)                            
                                SetCursorPosConstraintedInWorkArea(CursorLeft + 1, CursorTop,true,true,false);
                            _X = CursorLeft;
                            _Y = CursorTop;
                        }
                        break;

                    case ConsoleKey.DownArrow:
                        lock (ConsoleLock)
                        {
#if dbg
                            var line = _text[_currentLine];
                            System.Diagnostics.Debug.WriteLine($"_pastLine={_currentLine} height={_linesHeight[_currentLine]}");
                            System.Diagnostics.Debug.WriteLine($"{line}");
#endif
                            if (_currentLine < _text.Count-1)
                            {
                                if (CursorTop == _beginOfLineCurPos.Y + _linesHeight[_currentLine] - 1)
                                {
                                    if (_currentLine < _text.Count - 1)
                                    {
                                        _currentLine++;
                                        _X = 0;
                                        _Y++;
                                        _beginOfLineCurPos.X = _X;
                                        _beginOfLineCurPos.Y = _Y;
                                        SetCursorPos(_X, _Y);
                                    }
                                }
                                else
                                {
                                    _Y++;
                                    SetCursorPos(_X, _Y);
                                }
                            }
#if dbg
                            System.Diagnostics.Debug.WriteLine($"_currentLine={_currentLine} {_text[_currentLine]}");
#endif
                        }
                        break;

                    case ConsoleKey.UpArrow:
                        lock (ConsoleLock)
                        {
#if dbg
                            var line = _text[_currentLine];
                            System.Diagnostics.Debug.WriteLine($"_pastLine={_currentLine} height={_linesHeight[_currentLine]}");
                            System.Diagnostics.Debug.WriteLine($"{line}");
#endif
                            if (CursorTop == _beginOfLineCurPos.Y)
                            {
                                if (_currentLine > 0)
                                {
                                    _X = 0;
                                    _Y--;
                                    _currentLine--;
                                    _beginOfLineCurPos.X = _X;
                                    _beginOfLineCurPos.Y = _Y - (_linesHeight[_currentLine] - 1);
                                    SetCursorPos(_X, _Y);
                                }
                            }
                            else
                            {
                                _Y--;
                                SetCursorPos(_X, _Y);
                            }
#if dbg
                            System.Diagnostics.Debug.WriteLine($"_currentLine={_currentLine} {_text[_currentLine]}");
#endif
                        }
                        break;

                    default:
                        printable = true;
                        break;
                }
                if (c.Key==_cmdKey)
                {
                    printable = false;
                    SetStatusText("press a command key...");
                    printOnlyCursorInfo = false;
                    _cmdInput = true;
                }

                if (printable)
                {
                    if (_cmdInput)
                    {
                        switch (c.Key)
                        {
                            case ConsoleKey.Q:
                                end = true;
                                break;
                            default:
                                break;
                        }
                        printOnlyCursorInfo = false;
                        _cmdInput = false;
                    }
                }

                BackupCursorPos();
                ShowInfoBar(true, printOnlyCursorInfo);
                printOnlyCursorInfo = true;
                RestoreCursorPos();
            }
            Exit();
        }

        void Exit()
        {
            ClearScreen();
        }

        void SetStatusText(string text)
        {
            HideCur();
            BackupCursorPos();
            ClearInfoBar();
            _statusText = text;
            RestoreCursorPos();
            ShowCur();
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
            _linesHeight = new List<int>(_text.Count);
            for (int i = 0; i < _text.Count; i++) _linesHeight.Add(0);
            _fileEOL = platform;
        }

        void ShowFile()
        {
            lock (ConsoleLock)
            {
                int index = _firstLine;
                int y = 0;
                while (y < _height - _barHeight && index < _text.Count)
                {
                    var pos = CursorPos;
                    PrintLine(index++);
                    y = CursorTop;
                }
            }
        }

        void PrintLine(int index)
        {
            lock (ConsoleLock)
            {
                var y = CursorTop;
                Println(_text[index]);
                _linesHeight[index] = CursorTop - y;
            }
        }

        void ClearInfoBar()
        {
            lock (ConsoleLock)
            {
                SetCursorPos(0, _barY);
                Print($"{Invon} {Fillright}{Tdoff}");
                SetCursorPos(0, _barY + 1);
                Print($"{Invon} {Fillright}{Tdoff}");
            }
        }

        void ShowInfoBar(bool showCursor=true,bool onlyCursorInfo=false)
        {
            lock (ConsoleLock)
            {
                HideCur();
                if (!onlyCursorInfo)
                {
                    SetCursorPos(0, _barY);
                    if (!_cmdInput)
                        Print($"{Invon}{GetFileInfo()}{Tdoff}");
                    else
                        Print($"{Invon}{_statusText}{Tdoff}");
                    SetCursorPos(0, _barY + 1);
                    Print($"{Invon}{GetCmdsInfo()}{Tdoff}");
                }
                PrintCursorInfo();
                if (showCursor) ShowCur();
            }
        }

        void PrintCursorInfo()
        {
            SetCursorPos(80, _barY + 1);
            Print($"{Invon}{GetLastKeyInfo()} {GetPositionInfo()} | {GetCursorInfo()}    {Tdoff}");
        }

        string GetLastKeyInfo() => _lastKeyInfo.Key + ""; /*+$"({_lastKeyInfo.KeyChar})"*/
        string GetPositionInfo() => "line "+_currentLine+"";
        string GetCursorInfo() => $"{_X},{_Y}";
        string GetFileInfo()
        {
            return (_filePath == null) ? $"no file" : $"{_filePath.Name} | {Plur("line", _text.Count)} | size={HumanFormatOfSize(_filePath.FileInfo.Length,2)} | enc={_fileEncoding.EncodingName} | eol={FileEOL}";
        }

        string GetCmdsInfo()
        {
            string Opt(string shortCut,bool addCmdKeyStr=true) => $"{Bwhite}{Black}{(addCmdKeyStr?_cmdKeyStr:"")}{shortCut}{ColorSettings.Default}";
            return $"{Opt("l")} Load | {Opt("s")} Save | {Opt("t")} Top | {Opt("b")} Bottom | {Opt("F1",false)} Help | {Opt("q")} Quit | ";
        }
    }
}
