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
        int _defaultBarHeight = 2;
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
        int _splitedLineIndex;
        int _cmdBarIndex;
        readonly int _maxCmdBarIndex = 1;
        bool _barVisible;

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
                DisplayEditor();
            }
        }

        void InitEditor()
        {
            _splitedLineIndex = 0;
            _firstLine = 0;
            _currentLine = 0;
            _X = 0;
            _Y = 0;
            _cmdKey = ConsoleKey.Escape;
            _barHeight = _defaultBarHeight;
            _cmdKeyStr = "Esc ";
            _cmdBarIndex=0;
            _barVisible = true;
        }

        void DisplayEditor()
        {
            try
            {
                lock (ConsoleLock)
                {
                    HideCur();
                    ClearScreen();
                    _width = sc.WindowWidth;
                    _height = sc.WindowHeight;
                    _barY = _height - (_barVisible ? _barHeight : 0);
                    //_X = _Y = _currentLine = _splitedLineIndex = 0;
                    SetCursorHome(); 
                    DisplayFile();
                    EmptyInfoBar();
                    DisplayInfoBar(false);
                    SetCursorPos(_X, _Y);
                    ShowCur();
                }
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
                            if (_splitedLineIndex == 0)
                            {
                                if (p.X > 0)
                                    SetCursorLeft(p.X - 1);                                
                            }
                            else
                            {
                                var x = p.X - 1;
                                if (x < left)
                                {
                                    SetCursorPosConstraintedInWorkArea(right - 1, p.Y - 1, true, true, false);
                                    _splitedLineIndex--;
                                }
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
                            var curY = CursorTop;
                            if (index < line.Length-1)                            
                                SetCursorPosConstraintedInWorkArea(CursorLeft + 1, CursorTop,true,true,false);
                            _X = CursorLeft;
                            _Y = CursorTop;
                            if (CursorTop > curY) _splitedLineIndex++;
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
                                if (_linesHeight[_currentLine]==0)
                                {
                                    _linesHeight[_currentLine] = GetLineHeight(_currentLine, 0, _Y);
                                }

                                if (_splitedLineIndex == _linesHeight[_currentLine] - 1)
                                {
                                    _splitedLineIndex = 0;
                                    _currentLine++;
                                    _X = 0;
                                    _Y++;
                                    _beginOfLineCurPos.X = _X;
                                    _beginOfLineCurPos.Y = _Y;
                                }
                                else
                                {
                                    _Y++;
                                    _splitedLineIndex++;
                                }
                                if (_Y < _barY)
                                    SetCursorPos(_X, _Y);
                                else
                                {
                                    _Y = _barY - 1;
                                    if (_splitedLineIndex==0)
                                        _beginOfLineCurPos.Y = _Y;
                                    Scroll(-1);
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
                            if (_currentLine > 0 || _splitedLineIndex>0 )
                            {
                                if (_splitedLineIndex == 0)
                                {
                                    _X = 0;
                                    _Y--;
                                    _currentLine--;
                                    _beginOfLineCurPos.X = _X;
                                    _beginOfLineCurPos.Y = _Y - (_linesHeight[_currentLine] - 1);
                                    _splitedLineIndex = _linesHeight[_currentLine]-1;
                                }
                                else
                                {
                                    _splitedLineIndex--;
                                    _Y--;                                  
                                }
                                if (_Y >= 0)
                                {
                                    SetCursorPos(_X, _Y);
                                } else
                                {
                                    _Y = 0;
                                    _beginOfLineCurPos.Y = _Y - (_linesHeight[_currentLine] - 1);
                                    Scroll(1);
                                    SetCursorPos(_X, _Y);
                                }
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
                    if (_cmdInput)
                    {
                        if (_cmdBarIndex == _maxCmdBarIndex)
                        {
                            _cmdInput = false;
                            printable = false;
                            _statusText = null;
                            printOnlyCursorInfo = false;
                            _cmdInput = false;
                            _cmdBarIndex = 0;
                        } else
                        {
                            _cmdBarIndex++;
                        }
                    }
                    else _cmdInput = true;
                    if (_cmdInput)
                    {
                        printable = false;
                        SetStatusText($"press a command key (press {_cmdKeyStr.Trim()} for more commands ...");
                        printOnlyCursorInfo = false;
                        _cmdInput = true;
                    }
                }

                if (printable)
                {
                    if (_cmdInput)
                    {
                        switch (c.Key)
                        {
                            case ConsoleKey.I:
                                _barVisible = !_barVisible;
                                _barHeight = _barVisible ? _defaultBarHeight : 0;
                                DisplayEditor();
                                break;
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

                lock (ConsoleLock)
                {
                    BackupCursorPos();
                    DisplayInfoBar(false, printOnlyCursorInfo);
                    RestoreCursorPos();
                    ShowCur();
                    printOnlyCursorInfo = true;
                }
            }
            Exit();
        }

        void Scroll(int dy)
        {
            if (dy == 0) return;
            if (cons.WorkArea.IsEmpty)
            {
                lock (ConsoleLock)
                {
                    BackupCursorPos();
                    HideCur();

                    if (_barVisible && dy < 0)
                    {
                        SetCursorPos(0, _barY);
                        FillFromCursorRight();
                        SetCursorPos(0, _barY + 1);
                        FillFromCursorRight();
                    }
                    if (dy > 0)
                        ScrollWindowDown(dy);
                    else
                        ScrollWindowUp(-dy);

                    EmptyInfoBar();
                    DisplayInfoBar(false);

                    var line = _text[_currentLine];
                    var slines = GetWorkAreaStringSplits(line, _beginOfLineCurPos, true, false);
                    _linesHeight[_currentLine] = slines.Count;
                    if (dy < 0)
                    {
                        SetCursorPos(0, _barY - 1);
                        Print(slines[_splitedLineIndex].str);
                    } else
                    {
                        SetCursorPos(0, 0);
                        Print(slines[_splitedLineIndex].str);
                    }

                    RestoreCursorPos();
                    ShowCur();
                }
            }
            else
            {
                // TODO: workarea enabled mode
            }
        }

        void Exit()
        {
            ClearScreen();
        }

        void SetStatusText(string text)
        {
            HideCur();
            BackupCursorPos();
            EmptyInfoBar();
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

        void DisplayFile()
        {
            lock (ConsoleLock)
            {
                int index = _firstLine;
                int y = 0;
                var atBottom = false;
                while (y < _barY && index < _text.Count && !atBottom)
                {
                    var pos = CursorPos;
                    atBottom = PrintLine(index++);
                    y = CursorTop;
                }
            }
        }

        bool PrintLine(int index)
        {
            lock (ConsoleLock)
            {
                var y = CursorTop;
                var line = _text[index];
                var slines = GetWorkAreaStringSplits(line, new Point(0, y), true, false);
                int i = 0;
                while (i<slines.Count && y < _barY)
                {
                    SetCursorPos(0, y);
                    Print(slines[i++].str);
                    y++;
                }
                if (y < _barY) SetCursorPos(0, y);
                _linesHeight[index] = slines.Count; // CursorTop - y;
                return y >= _barY-1;
            }
        }

        int GetLineHeight(int lineIndex, int x,int y)
        {
            var slines = GetWorkAreaStringSplits(_text[lineIndex], new Point(x, y), true, false);
            return slines.Count;
        }

        void EmptyInfoBar()
        {
            if (!_barVisible) return;
            lock (ConsoleLock)
            {
                // all these remarks for 'auto line break' console mode
                // /!\ cursor visible leads to erase some characters (blank) in inverted mode and force ignore Tdoff !!
                // conclusion: invert mode switched is system bugged on windows -- avoid it
                SetCursorPos(0, _barY);
                EnableInvert();
                FillFromCursorRight();

                SetCursorPos(0, _barY + 1);
                FillFromCursorRight();
                //Print($"{Invon} {Fillright}{Tdoff}"); // TODO: fix this sentence do not print the last character line whereas this one does: Print($"{Invon}{Fillright}{Tdoff}");

                SetCursorPos(0, _barY + 1);
                DisableTextDecoration();
            }
        }

        void DisplayInfoBar(bool showCursor=true,bool onlyCursorInfo=false)
        {
            if (!_barVisible) return;
            lock (ConsoleLock)
            {
                var r = ActualWorkArea(false);
                CropX = r.Right-2;
                HideCur();

                if (!onlyCursorInfo)
                {
                    SetCursorPos(0, _barY);
                    EnableInvert();

                    if (!_cmdInput)
                    {                   // added { } has remove a bug of the print (disapear cmd line above when window less large than text on linux wsl ?!)
                        Print(GetFileInfo());
                    }
                    else
                        Print(_statusText);

                    SetCursorPos(0, _barY + 1);
                    Print(GetCmdsInfo());
                }

                PrintCursorInfo(onlyCursorInfo);
                
                CropX = -1;
                
                SetCursorPos(0, _barY);
                DisableTextDecoration();
                
                if (showCursor) ShowCur();
            }
        }

        void PrintCursorInfo(bool enableInvert=true)
        {
            SetCursorPos(0, _barY);
            EnableInvert();
            
            SetCursorPos(80, _barY + 1);
            Print($"{GetLastKeyInfo()} {GetPositionInfo()} | {_splitedLineIndex} | {GetCursorInfo()}    ");            
        }

        string GetLastKeyInfo() => _lastKeyInfo.Key + "";
        string GetPositionInfo() => "line "+_currentLine+"";
        string GetCursorInfo() => $"{_X},{_Y}";
        string GetFileInfo()
        {
            return (_filePath == null) ? $"no file" : $"{_filePath.Name} | {Plur("line", _text.Count)} | size={HumanFormatOfSize(_filePath.FileInfo.Length,2)} | enc={_fileEncoding.EncodingName} | eol={FileEOL}";
        }

        string GetCmdsInfo()
        {
            string Opt(string shortCut,bool addCmdKeyStr=true) => $"{Bwhite}{Black}{(addCmdKeyStr?_cmdKeyStr:"")}{shortCut}{ColorSettings.Default}";
            return _cmdBarIndex switch
            {
                1 => $" {Opt("i")} Toggle bar | {Opt("c")} Clear",
                _ => $" {Opt("q")} Quit | {Opt("l")} Load | {Opt("s")} Save | {Opt("t")} Top | {Opt("b")} Bottom | {Opt("F1", false)} Help",
            };
        }
    }
}
