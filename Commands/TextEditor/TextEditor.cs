//#define dbg

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
using System.IO;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;

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
        bool _readOnly;
        bool _fileModified;
        readonly int _defaultBarHeight = 2;
        ConsoleKeyInfo _lastKeyInfo;
        List<string> _text;
        List<List<StringSegment>> _linesSplits;
        Encoding _fileEncoding;
        OSPlatform? _fileEOL;
        string _eolSeparator;
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
        int _lastVisibleLineIndex;
        int _splitedLastVisibleLineIndex;
        long _fileSize;
        string _pressCmdKeyText;
        Stack<EditorBackup> _editorBackups;
        bool _rawMode;

        #endregion

        [Command("text editor")]
        public void Edit(
            [Parameter("path of an existing or of a new file. the path directory must exists",true)] FilePath filePath
            )
        {
            if (filePath==null || filePath.CheckPathExists())
            {
                InitEditor();
                LoadFile(filePath);
                DisplayEditor();
                WaitAndProcessKeyPress();
            }
        }

        void InitEditor(bool clearEditorBackups=true,bool forgetCurrentFile=true)
        {
            if (clearEditorBackups) _editorBackups = new Stack<EditorBackup>();
            _splitedLineIndex = 0;
            _firstLine = 0;
            _currentLine = 0;
            _X = 0;
            _Y = 0;
            _cmdKey = ConsoleKey.Escape;
            _cmdInput = false;
            _barHeight = _defaultBarHeight;
            _cmdKeyStr = "Esc ";
            _cmdBarIndex = 0;
            _barVisible = true;
            _pressCmdKeyText = $"press a command key - press {_cmdKeyStr.Trim()} for more commands ...";
            _statusText = null;
            if (forgetCurrentFile)
            {
                _eolSeparator = null;
                _fileModified = false;
                _readOnly = false;
                _fileSize = 0;
                _fileEOL = null;
                _fileEncoding = null;
                _filePath = null;
            }
            _text = new List<string> { "" };
            _linesSplits = new List<List<StringSegment>> { new List<StringSegment> { new StringSegment("", 0, 0, 0) } };
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
                    ComputeBarVisible();
                    SetCursorHome(); 
                    DisplayFile();
                    EmptyInfoBar();
                    DisplayInfoBar(false);
                    SetCursorPos(_X, _Y);
                    ShowCur();
                }                
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
                            if (_currentLine < _text.Count-1)
                            {
                                if (_linesSplits[_currentLine]==null)
                                {
                                    _linesSplits[_currentLine] = GetLineSplits(_currentLine, 0, _Y);
                                }

                                if (_splitedLineIndex == _linesSplits[_currentLine].Count - 1)
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
                            System.Diagnostics.Debug.WriteLine($"SCROLL DOWN: _lastVisibleLineIndex={_lastVisibleLineIndex} _splitedLastVisibleLineIndex={_splitedLastVisibleLineIndex} line={_text[_lastVisibleLineIndex]}");
#endif
                        }
                        break;

                    case ConsoleKey.UpArrow:
                        lock (ConsoleLock)
                        {
                            if (_currentLine > 0 || _splitedLineIndex>0 )
                            {
                                if (_splitedLineIndex == 0)
                                {
                                    _X = 0;
                                    _Y--;
                                    _currentLine--;
                                    _beginOfLineCurPos.X = _X;
                                    _beginOfLineCurPos.Y = _Y - (_linesSplits[_currentLine].Count - 1);
                                    _splitedLineIndex = _linesSplits[_currentLine].Count-1;
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
                                    _beginOfLineCurPos.Y = _Y - (_linesSplits[_currentLine].Count - 1);
                                    Scroll(1);
                                    SetCursorPos(_X, _Y);
                                }
                            }
#if dbg
                            System.Diagnostics.Debug.WriteLine($"SCROLL UP: _lastVisibleLineIndex={_lastVisibleLineIndex} _splitedLastVisibleLineIndex={_splitedLastVisibleLineIndex} line={_text[_lastVisibleLineIndex]}");
#endif
                        }
                        break;

                    case ConsoleKey.F1:
                        OpenHelp();
                        break;

                    default:
                        printable = true;
                        break;
                }

                if (c.Key==_cmdKey)
                {
                    if (!_barVisible)
                        ToggleBarVisibility();

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
                            if (_barVisible) ToggleBarVisibility();
                        } else
                        {
                            _cmdBarIndex++;
                        }
                    }
                    else _cmdInput = true;

                    if (_cmdInput)
                    {
                        printable = false;
                        _statusText = _pressCmdKeyText + " " + GetBarIndex();
                        printOnlyCursorInfo = false;
                        _cmdInput = true;
                    }
                }

                if (printable)
                {
                    if (_cmdInput)
                    {
                        var hideBar = true;
                        switch (c.Key)
                        {
                            case ConsoleKey.I:
                                // show file info bar
                                hideBar = false;
                                _statusText = null;                                
                                _cmdInput = false;
                                printOnlyCursorInfo = false;
                                lock (ConsoleLock) {
                                    BackupCursorPos();
                                    EmptyInfoBar();
                                    RestoreCursorPos();
                                }
                                break;

                            case ConsoleKey.V:
                                // toggle bar vis
                                ToggleBarVisibility();
                                break;

                            case ConsoleKey.T:
                                // file top
                                break;

                            case ConsoleKey.B:
                                // file bottom
                                break;

                            case ConsoleKey.C:
                                // clear editor
                                if (_readOnly) { hideBar = false; break; }
                                ClearCurrentEditor();
                                hideBar = false;
                                printOnlyCursorInfo = false;
                                break;

                            case ConsoleKey.N:
                                // new file
                                ClearCurrentEditor(true);
                                hideBar = false;
                                printOnlyCursorInfo = false;
                                break;

                            case ConsoleKey.S:
                                // save file
                                if (_readOnly) { hideBar = false; break; }
                                SaveFile();
                                break;

                            case ConsoleKey.L:
                                // load file
                                break;

                            case ConsoleKey.R:
                                _rawMode = !_rawMode;
                                _statusText = "raw mode is " + (_rawMode?"enabled":"disabled");
                                hideBar = false;
                                printOnlyCursorInfo = false;
                                RefreshEditor();
                                break;

                            case ConsoleKey.Q:
                                // quit current editor - unstack to previous file if any, else exit
                                if (_fileModified && Confirm($"file '{_filePath.Name}' has unsaved changes. Do you want to save it"))
                                    SaveFile();
                                if (_editorBackups.Count == 0)
                                    end = true;
                                else
                                {
                                    hideBar = false;
                                    printOnlyCursorInfo = false;
                                    RestorePreviousFile();
                                }
                                break;

                            default:
                                // invalid
                                hideBar = false;
                                _statusText = $"{Bred}Invalid comand key.{ColorSettings.Default} {_pressCmdKeyText} " + GetBarIndex();
                                printOnlyCursorInfo = false;
                                break;
                        }
                        if (hideBar)
                        {
                            if (_barVisible) ToggleBarVisibility();
                            _cmdInput = false;
                            printOnlyCursorInfo = false;
                        }
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

        void RefreshEditor()
        {
            lock (ConsoleLock)
            {
                BackupCursorPos();
                DisplayEditor();
                RestoreCursorPos();
            }
        }

        bool SaveFile()
        {
            lock (ConsoleLock)
            {
                BackupCursorPos();
                try
                {
                    string text = string.Join((_eolSeparator == null) ? "" : _eolSeparator, _text);
                    File.WriteAllText(_filePath.FullName, text);
                    _fileModified = false;
                    UpdateFileInfoBar();
                } catch (Exception ex) {
                    Error(ex.Message);
                    return false;
                }
                finally
                {
                    RestoreCursorPos();
                }
                return true;
            }
        }

        void UpdateFileInfoBar()
        {
            if (_barVisible && _cmdInput && _statusText == null)
            {
                lock (ConsoleLock) {
                    BackupCursorPos();
                    DisplayInfoBar();
                    RestoreCursorPos();
                }
            }
        }

        void Error(string text)
        {
            lock (ConsoleLock)
            {
                var bVis = ShowEmptyBar();
                PrintBarMessage(Bred + text + ". Press a key to continue..."+ColorSettings.Default);
                var c = sc.ReadKey(true);
                if (bVis) ToggleBarVisibility();
            }
        }

        bool Confirm(string text)
        {
            lock (ConsoleLock)
            {
                var bVis = ShowEmptyBar();
                PrintBarMessage(text + " ? [Y|y|N|n]: ");
                var c = sc.ReadKey();
                if (Char.IsLetterOrDigit(c.KeyChar))
                {
                    EnableInvert();
                    Print(c + "");
                }
                DisableTextDecoration();
                var s = c.KeyChar.ToString().ToLower();
                if (!bVis) ToggleBarVisibility();
                return s=="y";
            }
        }

        void PrintBarMessage(string text)
        {
            SetCursorPos(1, _barY);
            EnableInvert();
            Print(text);
            DisableTextDecoration();
        }

        bool ShowEmptyBar()
        {
            var bVis = _barVisible;
            if (!_barVisible) ToggleBarVisibility();
            EmptyInfoBar();
            return bVis;
        }

        void OpenHelp()
        {
            _editorBackups.Push(GetCurrentEditorBackup());
            lock (ConsoleLock)
            {
                InitEditor(false);
                if (LoadFile(new FilePath(Path.Combine(
                    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                    "Commands",
                    "TextEditor",
                    "edit-help.txt")))) {
                    _readOnly = true;
                    _barVisible = true;
                    ComputeBarVisible();
                    DisplayEditor();
                } else
                {
                    RestorePreviousFile();
                }
            }
        }

        void RestorePreviousFile()
        {
            var editorBackup = _editorBackups.Pop();
            InitEditor(false);
            RestoreEditorBackup(editorBackup);
            DisplayEditor();
        }

        void ClearCurrentEditor(bool newFile=false)
        {
            lock (ConsoleLock)
            {
                InitEditor(false,newFile);
                _fileModified = !newFile;
                _fileSize = 0;
                DisplayEditor();
            }
        }

        void RestoreEditorBackup(EditorBackup editorBackup)
        {
            _text.Clear();
            _linesSplits.Clear();
            _rawMode = editorBackup.RawMode;
            _eolSeparator = editorBackup.EOLSeparator;
            _filePath = editorBackup.FilePath;
            _firstLine = editorBackup.FirstLine;
            _fileSize = editorBackup.FileSize;
            _readOnly = editorBackup.ReadOnly;
            _fileModified = editorBackup.FileModified;
            _currentLine = editorBackup.CurrentLine;
            _X = editorBackup.X;
            _Y = editorBackup.Y;
            _text.AddRange(editorBackup.Text);
            _linesSplits.AddRange(editorBackup.LinesSplits);
            _fileEncoding = editorBackup.FileEncoding;
            _fileEOL = editorBackup.FileEOL;
            _beginOfLineCurPos = editorBackup.BeginOfLineCurPos;
            _lastVisibleLineIndex = editorBackup.LastVisibleLineIndex;
            _splitedLastVisibleLineIndex = editorBackup.SplitedLastVisibleLineIndex;
            _barVisible = true;
            ComputeBarVisible();
        }

        void ComputeBarVisible()
        {
            _barHeight = _barVisible ? _defaultBarHeight : 0;
            _barY = _height - (_barVisible ? _barHeight : 0);
        }

        void ToggleBarVisibility()
        {
#if dbg
            System.Diagnostics.Debug.WriteLine($"PRE: _lastVisibleLineIndex={_lastVisibleLineIndex} _splitedLastVisibleLineIndex={_splitedLastVisibleLineIndex} line={_text[_lastVisibleLineIndex]}");
#endif
            var setVisible = false;
            lock (ConsoleLock)
            {
                if (_barVisible)
                {
                    HideCur();
                    EraseInfoBar();
                    if (_lastVisibleLineIndex < _text.Count-1)
                    {
                        var slines = GetWorkAreaStringSplits(_text[_lastVisibleLineIndex], new Point(_X, _Y), true, false).Splits;
                        var y = _barY;
                        var newBarY = _barY + _barHeight;
                        SetCursorPos(_X, _barY);
                        bool atBottom;
                        int splitedLineIndex;
                        for (int i = 0; i < 2; i++)
                        {
                            if (slines.Count == 1 || _splitedLastVisibleLineIndex == slines.Count - 1)
                            {
                                if (i == 0) _lastVisibleLineIndex++;
                                if (_lastVisibleLineIndex < _text.Count-1)
                                {
                                    (atBottom, splitedLineIndex, slines) = PrintLine(_lastVisibleLineIndex, 0, newBarY);
                                    if (i == 1) break;
                                    if (splitedLineIndex == _barHeight)
                                    {
                                        _splitedLastVisibleLineIndex = splitedLineIndex;
                                        break;
                                    }
                                    else
                                    {
                                        _lastVisibleLineIndex++;
                                        _splitedLastVisibleLineIndex = 0;
                                    }
                                }
                            }
                            else
                            {
                                if (i == 0) _splitedLastVisibleLineIndex++;
                                (atBottom, splitedLineIndex, slines) = PrintLine(_lastVisibleLineIndex, _splitedLastVisibleLineIndex, newBarY);
                                if (i == 1) break;
                                if (splitedLineIndex == slines.Count - 1)
                                {
                                    _splitedLastVisibleLineIndex = 0;
                                    _lastVisibleLineIndex++;
                                }
                                else
                                    _splitedLastVisibleLineIndex = 0;
                            }
                        }
                    }
                    SetCursorPos(_X, _Y);
                    ShowCur();
                }
                else
                {
                    setVisible = true;
                }
            }
            _barVisible = !_barVisible;
            ComputeBarVisible();
            if (setVisible)
            {
                lock (ConsoleLock)
                {
                    BackupCursorPos();
                    DisplayInfoBar();
                    RestoreCursorPos();
                    DecrementLineYPosition(2);
                }
            }
#if dbg
            System.Diagnostics.Debug.WriteLine($"POST: _lastVisibleLineIndex={_lastVisibleLineIndex} _splitedLastVisibleLineIndex={_splitedLastVisibleLineIndex} line={_text[_lastVisibleLineIndex]}");
#endif
        }

        void DecrementLineYPosition(int count=1)
        {
            for (int i=0;i<count;i++)
            {                
                if (_splitedLastVisibleLineIndex > 0)
                    _splitedLastVisibleLineIndex--;
                else
                {
                    if (_lastVisibleLineIndex > 0)
                    {
                        _lastVisibleLineIndex--;
                        _splitedLastVisibleLineIndex = _linesSplits[_lastVisibleLineIndex].Count - 1;
                    }
                }
            }
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
                        EraseInfoBar();
                    }
                    if (dy > 0)
                        ScrollWindowDown(dy);
                    else
                        ScrollWindowUp(-dy);

                    EmptyInfoBar();
                    DisplayInfoBar(false);

                    var line = _text[_currentLine];
                    var slines = GetWorkAreaStringSplits(line, _beginOfLineCurPos, true, false).Splits;
                    _linesSplits[_currentLine] = slines;
                    if (dy < 0)
                    {
                        SetCursorPos(0, _barY - 1);
                        PrintLineSplit(slines[_splitedLineIndex].Text,_splitedLastVisibleLineIndex==slines.Count-1);
                        _lastVisibleLineIndex = _currentLine;
                        _splitedLastVisibleLineIndex = _splitedLineIndex;
                    } else
                    {
                        SetCursorPos(0, 0);
                        PrintLineSplit(slines[_splitedLineIndex].Text, _splitedLineIndex==slines.Count-1);
                        DecrementLineYPosition();
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

        void SetCursorHome()
        {
            _X = 0;
            _Y = 0;
            CursorHome();
        }

        bool LoadFile(FilePath filePath)
        {
            if (filePath == null) return true;
            try
            {
                _filePath = filePath;
                _fileSize = filePath.FileInfo.Length;
                _readOnly = filePath.FileInfo.Attributes.HasFlag(FileAttributes.ReadOnly);
                _fileEncoding = filePath.GetEncoding(Encoding.Default);
                var (lines, platform, eolSeparator) = FIleReader.ReadAllLines(filePath.FullName);
                _text = lines.ToList();
                _eolSeparator = eolSeparator;
                _linesSplits = new List<List<StringSegment>>(_text.Count);
                for (int i = 0; i < _text.Count; i++) _linesSplits.Add(null);
                _fileEOL = platform;
                return true;
            } catch (Exception ex)
            {
                Error(ex.Message);
                return false;
            }
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
                    var r = PrintLine(index++);
                    atBottom = r.atBottom;
                    var splitedLastLineIndex = r.splitedLineIndex;
                    _lastVisibleLineIndex = index-1;
                    _splitedLastVisibleLineIndex = splitedLastLineIndex;
                    y = CursorTop;
                }
            }
#if dbg
            System.Diagnostics.Debug.WriteLine($"INIT: _lastVisibleLineIndex={_lastVisibleLineIndex} _splitedLastVisibleLineIndex={_splitedLastVisibleLineIndex} line={_text[_lastVisibleLineIndex]}");
#endif
        }

        EditorBackup GetCurrentEditorBackup()
        {
            return new EditorBackup(
                _rawMode,
                _filePath,
                _eolSeparator,
                _readOnly,
                _fileModified,
                _fileSize,
                _firstLine,
                _currentLine,
                _X,
                _Y,
                _text,
                _linesSplits,
                _fileEncoding,
                _fileEOL,
                _beginOfLineCurPos,
                _lastVisibleLineIndex,
                _splitedLastVisibleLineIndex
                );
        }

        (bool atBottom,int splitedLineIndex, List<StringSegment> slines) PrintLine(int index,int subIndex=0,int maxY=-1)
        {
            if (maxY == -1) maxY = _barY;
            lock (ConsoleLock)
            {
                var y = CursorTop;
                var line = _text[index];
                
                var slines = GetWorkAreaStringSplits(line, new Point(0, y), true, false, !_rawMode).Splits;

                int i = subIndex;
                while (i<slines.Count && y < maxY)
                {
                    SetCursorPos(0, y);                    
                    PrintLineSplit(slines[i].Text,i== slines.Count-1);
                    y++;
                    i++;
                }
                if (y < maxY) SetCursorPos(0, y);
                _linesSplits[index] = slines;
                var atBottom = y >= maxY;
                return (atBottom,i-1,slines);
            }
        }

        void PrintLineSplit(string s,bool eol)
        {
            Print(s, false, _rawMode);
            if (!_rawMode && eol) Print(ColorSettings.Default.ToString());
        }

        List<StringSegment> GetLineSplits(int lineIndex, int x,int y) => GetWorkAreaStringSplits(_text[lineIndex], new Point(x, y), true, false).Splits;

        void EraseInfoBar()
        {
            if (!_barVisible) return;
            SetCursorPos(0, _barY);
            FillFromCursorRight();
            SetCursorPos(0, _barY + 1);
            FillFromCursorRight();
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

                    if (_statusText == null)
                    {                   // added { } has remove a bug of the print (disapear cmd line above when window less large than text on linux wsl ?!)
                        SetCursorPos(1, _barY);
                        Print(GetFileInfo());
                    }
                    else
                    {
                        EmptyInfoBar();
                        SetCursorPos(1, _barY);
                        EnableInvert();
                        Print(_statusText);
                    }

                    if (_cmdInput)
                    {
                        SetCursorPos(0, _barY + 1);
                        Print(GetCmdsInfo());
                    }
                }

                if (!_cmdInput) PrintCursorInfo();
                
                CropX = -1;
                
                SetCursorPos(0, _barY);
                DisableTextDecoration();
                
                if (showCursor) ShowCur();
            }
        }

        void PrintCursorInfo()
        {
            SetCursorPos(1, _barY+ 1 );
            EnableInvert();            
            Print($"{GetPositionInfo()} | {_splitedLineIndex} | {GetCursorInfo()} | [{GetLastKeyInfo()}]       ");            
        }

        string GetBarIndex() => $"({_cmdBarIndex}/{_maxCmdBarIndex})";
        string GetLastKeyInfo() => _lastKeyInfo.Key + "";
        string GetPositionInfo() => "line "+(_currentLine+1)+"";
        string GetCursorInfo() => $"{_X},{_Y}";
        string GetFileInfo()
        {
            return (_filePath == null) ? $"no file" : $"{_filePath.Name}{(_fileModified?"*":"")} | {Plur("line", _text.Count)} | size={HumanFormatOfSize(_fileSize,2)} | enc={_fileEncoding.EncodingName} | eol={FileEOL}";
        }
        string GetCmdsInfo()
        {
            string ShcutOpt(string shortCut,bool ifNotReadOnly=false,bool addCmdKeyStr=true) => $"{((ifNotReadOnly && _readOnly) ? Bwhite:Bwhite)}{( (ifNotReadOnly&&_readOnly) ?Gray:Black)}{(addCmdKeyStr?_cmdKeyStr:"")}{shortCut}{ColorSettings.Default}";
            string Opt(string shortCut,string label, bool ifNotReadOnly = false, bool addCmdKeyStr = true) => $"{ShcutOpt(shortCut, ifNotReadOnly, addCmdKeyStr)} {((ifNotReadOnly&&_readOnly)?$"{Bgray}":"")}{label}{ColorSettings.Default}";
            return _cmdBarIndex switch
            {
                1 => $" {Opt("t", "Top")} | {Opt("b", "Bottom")} | {Opt("c", "Clear",true)} | {Opt("n", "New")} | {Opt("r", "Toggle raw mode")}",
                _ => $" {Opt("q", "Quit")} | {Opt("l", "Load")} | {Opt("s", "Save",true)} | {Opt("v", "Toggle bar")} | {Opt("i", "Info bar")} | {Opt("F1", "Help",false,false)}",
            };
        }
        void BackupCursorPos() => _bkCursorPos = CursorPos;
        void RestoreCursorPos() => SetCursorPos(_bkCursorPos);

    }
}
