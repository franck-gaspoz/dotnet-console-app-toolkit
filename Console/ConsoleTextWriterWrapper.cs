using DotNetConsoleAppToolkit.Component.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.CompilerServices;
using static DotNetConsoleAppToolkit.DotNetConsole;

namespace DotNetConsoleAppToolkit.Console
{
    public class ConsoleTextWriterWrapper : TextWriterWrapper
    {
        #region attributes
        
        public bool RedirecToErr = false;

        #region console output settings

        public int CropX = -1;
        public bool EnableConstraintConsolePrintInsideWorkArea = false;
        public bool EnableFillLineFromCursor = true;

        protected int _cursorLeftBackup;
        protected int _cursorTopBackup;
        protected ConsoleColor _backgroundBackup = ConsoleColor.Black;
        protected ConsoleColor _foregroundBackup = ConsoleColor.White;
        protected Dictionary<string, CommandDelegate> _drtvs;

        #endregion

        #endregion

        #region construction & init

        public ConsoleTextWriterWrapper() : base() { }

        public ConsoleTextWriterWrapper(TextWriter textWriter) : base(textWriter) { }

        void Init()
        {
            _drtvs = new Dictionary<string, CommandDelegate>() {
                { PrintDirectives.bkf+""   , (x) => RelayCall(BackupForeground) },
                { PrintDirectives.bkb+""   , (x) => RelayCall(BackupBackground) },
                { PrintDirectives.rsf+""   , (x) => RelayCall(RestoreForeground) },
                { PrintDirectives.rsb+""   , (x) => RelayCall(RestoreBackground) },

                { PrintDirectives.f+"="    , (x) => RelayCall(() => SetForeground( TextColor.ParseColor(x))) },
                { PrintDirectives.f8+"="   , (x) => RelayCall(() => SetForeground( TextColor.Parse8BitColor(x))) },
                { PrintDirectives.f24+"="  , (x) => RelayCall(() => SetForeground( TextColor.Parse24BitColor(x))) },
                { PrintDirectives.b+"="    , (x) => RelayCall(() => SetBackground( TextColor.ParseColor(x))) },
                { PrintDirectives.b8+"="   , (x) => RelayCall(() => SetBackground( TextColor.Parse8BitColor(x))) },
                { PrintDirectives.b24+"="  , (x) => RelayCall(() => SetBackground( TextColor.Parse24BitColor(x))) },

                { PrintDirectives.df+"="   , (x) => RelayCall(() => SetDefaultForeground( TextColor.ParseColor(x))) },
                { PrintDirectives.db+"="   , (x) => RelayCall(() => SetDefaultBackground( TextColor.ParseColor(x))) },
                { PrintDirectives.rdc+""   , (x) => RelayCall(RestoreDefaultColors)},

                { PrintDirectives.cls+""   , (x) => RelayCall(() => ClearScreen()) },
                { PrintDirectives.br+""    , (x) => RelayCall(LineBreak) },
                { PrintDirectives.inf+""   , (x) => RelayCall(Infos) },
                { PrintDirectives.bkcr+""  , (x) => RelayCall(BackupCursorPos) },
                { PrintDirectives.rscr+""  , (x) => RelayCall(RestoreCursorPos) },
                { PrintDirectives.crh+""   , (x) => RelayCall(HideCur) },
                { PrintDirectives.crs+""   , (x) => RelayCall(ShowCur) },
                { PrintDirectives.crx+"="  , (x) => RelayCall(() => SetCursorLeft(GetCursorX(x))) },
                { PrintDirectives.cry+"="  , (x) => RelayCall(() => SetCursorTop(GetCursorY(x))) },
                { PrintDirectives.exit+""  , (x) => RelayCall(() => Exit()) },
                { PrintDirectives.exec+"=" , (x) => ExecCSharp((string)x) },

                { PrintDirectives.invon+"" , (x) => RelayCall(EnableInvert) },
                { PrintDirectives.lion+"" , (x) => RelayCall(EnableLowIntensity) },
                { PrintDirectives.uon+"" , (x) => RelayCall(EnableUnderline) },
                { PrintDirectives.bon+"" , (x) => RelayCall(EnableBold) },
                { PrintDirectives.blon+"" , (x) => RelayCall(EnableBlink) },
                { PrintDirectives.tdoff+"" , (x) => RelayCall(DisableTextDecoration) },

                { PrintDirectives.cl+"" , (x) => RelayCall(ClearLine) },
                { PrintDirectives.clright+"" , (x) => RelayCall(ClearLineFromCursorRight) },
                { PrintDirectives.fillright+"" , (x) => RelayCall(() => FillFromCursorRight()) },
                { PrintDirectives.clleft+"" , (x) => RelayCall(ClearLineFromCursorLeft) },

                { PrintDirectives.cup+"" , (x) => RelayCall(() => MoveCursorTop(1)) },
                { PrintDirectives.cdown+"" , (x) => RelayCall(() => MoveCursorDown(1)) },
                { PrintDirectives.cleft+"" , (x) => RelayCall(() => MoveCursorLeft(1)) },
                { PrintDirectives.cright+"" , (x) => RelayCall(() => MoveCursorRight(1)) },
                { PrintDirectives.chome+"" , (x) => RelayCall(CursorHome) },

                { PrintDirectives.cnup+"=" , (x) => RelayCall(() => MoveCursorTop(Convert.ToInt32(x))) },
                { PrintDirectives.cndown+"=" , (x) => RelayCall(() => MoveCursorDown(Convert.ToInt32(x))) },
                { PrintDirectives.cnleft+"=" , (x) => RelayCall(() => MoveCursorLeft(Convert.ToInt32(x))) },
                { PrintDirectives.cnright+"=" , (x) => RelayCall(() => MoveCursorRight(Convert.ToInt32(x))) },
            };
        }

        #endregion

        #region buffering operations

        public void EnableBuffer()
        {
            lock (Lock)
            {
                _buffer.Clear();
                IsBufferEnabled = true;
            }
        }

        public void CloseBuffer()
        {
            lock (Lock)
            {
                var txt = _buffer.ToString();
                //Print(txt);
                _buffer.Clear();
                IsBufferEnabled = false;
            }
        }

        #endregion

        #region console output operations

        protected delegate object CommandDelegate(object x);
        
        object RelayCall(Action method) { method(); return null; }

        public void CursorHome() => Locked(() => { Print($"{(char)27}[H"); });
        public void ClearLineFromCursorRight() => Locked(() => { Print($"{(char)27}[K"); });
        public void ClearLineFromCursorLeft() => Locked(() => { Print($"{(char)27}[1K"); });
        public void ClearLine() => Locked(() => { Print($"{(char)27}[2K"); });

        public void FillFromCursorRight()
        {
            lock (Lock)
            {
                FillLineFromCursor(' ', false, false);
            }
        }

        public void EnableInvert() => Locked(() => { Print($"{(char)27}[7m"); });
        public void EnableBlink() => Locked(() => { Print($"{(char)27}[5m"); });           // not available on many consoles
        public void EnableLowIntensity() => Locked(() => { Print($"{(char)27}[2m"); });    // not available on many consoles
        public void EnableUnderline() => Locked(() => { Print($"{(char)27}[4m"); });
        public void EnableBold() => Locked(() => { Print($"{(char)27}[1m"); });            // not available on many consoles
        public void DisableTextDecoration() => Locked(() => { Print($"{(char)27}[0m"); RestoreDefaultColors(); });

        public void MoveCursorDown(int n = 1) => Locked(() => { Print($"{(char)27}[{n}B"); });
        public void MoveCursorTop(int n = 1) => Locked(() => { Print($"{(char)27}[{n}A"); });
        public void MoveCursorLeft(int n = 1) => Locked(() => { Print($"{(char)27}[{n}D"); });
        public void MoveCursorRight(int n = 1) => Locked(() => { Print($"{(char)27}[{n}C"); });

        public void ScrollWindowDown(int n = 1) { _textWriter.Write(((char)27) + $"[{n}T"); }
        public void ScrollWindowUp(int n = 1) { _textWriter.Write(((char)27) + $"[{n}S"); }

        public void BackupForeground() => Locked(() => _foregroundBackup = _textWriter.ForegroundColor);
        public void BackupBackground() => Locked(() => _backgroundBackup = _textWriter.BackgroundColor);
        public void RestoreForeground() => Locked(() => _textWriter.ForegroundColor = _foregroundBackup);
        public void RestoreBackground() => Locked(() => _textWriter.BackgroundColor = _backgroundBackup);

        /// <summary>
        /// set foreground color from a 3 bit palette color (ConsoleColor)
        /// </summary>
        /// <param name="c"></param>
        public void SetForeground(ConsoleColor c) => Locked(() => _textWriter.ForegroundColor = c);

        /// <summary>
        /// set foreground color from a 8 bit palette color (vt/ansi)
        /// </summary>
        /// <param name="c"></param>
        public void SetForeground(int c) => Locked(() =>
        {
            Print($"{(char)27}[38;5;{c}m");
        });

        /// <summary>
        /// set background color from a 8 bit palette color (vt/ansi)
        /// </summary>
        /// <param name="c"></param>
        public void SetBackground(int c) => Locked(() =>
        {
            Print($"{(char)27}[48;5;{c}m");
        });

        /// <summary>
        /// set foreground color from a 24 bit palette color (vt/ansi)
        /// </summary>
        /// <param name="r">red from 0 to 255</param>
        /// <param name="g">green from 0 to 255</param>
        /// <param name="b">blue from 0 to 255</param>
        public void SetForeground(int r, int g, int b) => Locked(() =>
        {
            Print($"{(char)27}[38;2;{r};{g};{b}m");
        });

        /// <summary>
        /// set background color from a 24 bit palette color (vt/ansi)
        /// </summary>
        /// <param name="r">red from 0 to 255</param>
        /// <param name="g">green from 0 to 255</param>
        /// <param name="b">blue from 0 to 255</param>
        public void SetBackground(int r, int g, int b) => Locked(() =>
        {
            Print($"{(char)27}[48;2;{r};{g};{b}m");
        });

        public void SetForeground((int r, int g, int b) color) => SetForeground(color.r, color.g, color.b);
        public void SetBackground((int r, int g, int b) color) => SetBackground(color.r, color.g, color.b);
        public void SetBackground(ConsoleColor c) => Locked(() => _textWriter.BackgroundColor = c);

        public void SetDefaultForeground(ConsoleColor c) => Locked(() => DefaultForeground = c);
        
        public void SetDefaultBackground(ConsoleColor c) => Locked(() => DefaultBackground = c);
        
        public void RestoreDefaultColors() => Locked(() => { _textWriter.ForegroundColor = DefaultForeground; _textWriter.BackgroundColor = DefaultBackground; });
        
        public void ClearScreen()
        {
            Locked(() =>
            {
                _textWriter.Clear();
                RestoreDefaultColors();
                UpdateUI(true, false);
            }
            );
        }
        
        public void LineBreak()
        {
            Locked(() =>
            {
                ConsolePrint(string.Empty, true);
            });
        }

        public void BackupCursorPos()
        {
            Locked(() =>
            {
                _cursorLeftBackup = _textWriter.CursorLeft;
                _cursorTopBackup = _textWriter.CursorTop;
            });
        }
        
        public void RestoreCursorPos()
        {
            Locked(() =>
            {
                _textWriter.CursorLeft = _cursorLeftBackup;
                _textWriter.CursorTop = _cursorTopBackup;
            });
        }
        
        public void SetCursorLeft(int x) => Locked(() => _textWriter.CursorLeft = FixX(x));
        
        public void SetCursorTop(int y) => Locked(() => _textWriter.CursorTop = FixY(y));
        
        public int CursorLeft
        {
            get { lock (Lock) { return _textWriter.CursorLeft; } }
        }
        
        public int CursorTop
        {
            get { lock (Lock) { return _textWriter.CursorTop; } }
        }
        
        public Point CursorPos
        {
            get
            {
                lock (Lock)
                { return new Point(CursorLeft, CursorTop); }
            }
        }
        
        public void SetCursorPos(Point p)
        {
            lock (Lock)
            {
                var x = p.X;
                var y = p.Y;
                FixCoords(ref x, ref y);
                _textWriter.CursorLeft = x;
                _textWriter.CursorTop = y;
            }
        }
        
        public void SetCursorPos(int x, int y)
        {
            lock (Lock)
            {
                FixCoords(ref x, ref y);
                _textWriter.CursorLeft = x;
                _textWriter.CursorTop = y;
            }
        }
        
        public bool CursorVisible => _textWriter.CursorVisible;
        
        public void HideCur() => Locked(() => _textWriter.CursorVisible = false);
        
        public void ShowCur() => Locked(() => _textWriter.CursorVisible = true);              

        public void SetWorkArea(string id, int wx, int wy, int width, int height)
        {
            lock (Lock)
            {
                _workArea = new WorkArea(id, wx, wy, width, height);
                ApplyWorkArea();
                EnableConstraintConsolePrintInsideWorkArea = true;
            }
        }

        public void UnsetWorkArea()
        {
            _workArea = new WorkArea();
            EnableConstraintConsolePrintInsideWorkArea = false;
        }

        public ActualWorkArea ActualWorkArea(bool fitToVisibleArea = true)
        {
            var x0 = _workArea.Rect.IsEmpty ? 0 : _workArea.Rect.X;
            var y0 = _workArea.Rect.IsEmpty ? 0 : _workArea.Rect.Y;
            var w0 = _workArea.Rect.IsEmpty ? -1 : _workArea.Rect.Width;
            var h0 = _workArea.Rect.IsEmpty ? -1 : _workArea.Rect.Height;
            var (x, y, w, h) = GetCoords(x0, y0, w0, h0, fitToVisibleArea);
            return new ActualWorkArea(_workArea.Id, x, y, w, h);
        }

        void ApplyWorkArea(bool viewSizeChanged = false)
        {
            if (_workArea.Rect.IsEmpty) return;
            lock (Lock)
            {
                if (ViewResizeStrategy != ViewResizeStrategy.HostTerminalDefault &&
                    (!viewSizeChanged ||
                    (viewSizeChanged && ViewResizeStrategy == ViewResizeStrategy.FitViewSize)))
                    try
                    {
                        _textWriter.WindowTop = 0;
                        _textWriter.WindowLeft = 0;
                        _textWriter.BufferWidth = _textWriter.WindowWidth;
                        _textWriter.BufferHeight = _textWriter.WindowHeight;
                    }
                    catch (Exception) { }
            }
        }

        public void SetCursorAtWorkAreaTop()
        {
            if (_workArea.Rect.IsEmpty) return;     // TODO: set cursor even if workarea empty?
            lock (Lock)
            {
                SetCursorPos(_workArea.Rect.X, _workArea.Rect.Y);
            }
        }

        public string GetPrint(
            string s,
            bool lineBreak = false,
            bool doNotEvaluatePrintDirectives = false,      // TODO: remove this parameter
            bool ignorePrintDirectives = false,
            PrintSequences printSequences = null)
        {
            lock (Lock)
            {
                lock (Lock)
                {
                    if (string.IsNullOrWhiteSpace(s)) return s;
                    var ms = new MemoryStream(s.Length * 4);
                    var sw = new StreamWriter(ms);
                    Redirect(sw);
                    var e = EnableConstraintConsolePrintInsideWorkArea;
                    EnableConstraintConsolePrintInsideWorkArea = false;
                    Print(s, lineBreak, false, !ignorePrintDirectives, true, printSequences);
                    EnableConstraintConsolePrintInsideWorkArea = e;
                    sw.Flush();
                    ms.Position = 0;
                    var rw = new StreamReader(ms);
                    var txt = rw.ReadToEnd();
                    rw.Close();
                    Redirect((StreamWriter)null);
                    return txt;
                }
            }
        }

        public string GetPrintWithEscapeSequences(string s, bool lineBreak = false)
        {
            lock (Lock)
            {
                if (string.IsNullOrWhiteSpace(s)) return s;
                var ms = new MemoryStream(s.Length * 4);
                var sw = new StreamWriter(ms);
                Redirect(sw);
                var e = EnableConstraintConsolePrintInsideWorkArea;
                EnableConstraintConsolePrintInsideWorkArea = false;
                Print(s, lineBreak);
                EnableConstraintConsolePrintInsideWorkArea = e;
                sw.Flush();
                ms.Position = 0;
                var rw = new StreamReader(ms);
                var txt = rw.ReadToEnd();
                rw.Close();
                Redirect((StreamWriter)null);
                return txt;
            }
        }
                
        public void ConsolePrint(string s, bool lineBreak = false)
        {
            // any print goes here...
            lock (Lock)
            {
                if (CropX == -1)
                    ConsoleSubPrint(s, lineBreak);
                else
                {
                    var x = CursorLeft;
                    var mx = Math.Max(x, CropX);
                    if (mx > x)
                    {
                        var n = mx - x + 1;
                        if (s.Length <= n)
                            ConsoleSubPrint(s, lineBreak);
                        else
                            ConsoleSubPrint(s.Substring(0, n), lineBreak);
                    }
                }
            }
        }

        public override void FileEcho(
            string s,
            bool lineBreak = false,
            [CallerMemberName]string callerMemberName = "",
            [CallerLineNumber]int callerLineNumber = -1)
        {
            if (!FileEchoEnabled) return;
            if (FileEchoDumpDebugInfo)
                _echoStreamWriter?.Write($"x={CursorLeft},y={CursorTop},l={s.Length},w={_textWriter.WindowWidth},h={_textWriter.WindowHeight},wtop={_textWriter.WindowTop} bw={_textWriter.BufferWidth},bh={_textWriter.BufferHeight},br={lineBreak} [{callerMemberName}:{callerLineNumber}] :");
            _echoStreamWriter?.Write(s);
            if (lineBreak | FileEchoAutoLineBreak) _echoStreamWriter?.WriteLine(string.Empty);
            if (FileEchoAutoFlush) _echoStreamWriter?.Flush();
        }

        public override void Write(string s)
        {
            if (RedirecToErr)
                Err.Write(s);
            else
                base.Write(s);
        }

        void Print(
            object s,
            bool lineBreak = false,
            bool preserveColors = false,
            bool parseCommands = true,
            bool doNotEvalutatePrintDirectives = false,
            PrintSequences printSequences = null)
        {
            lock (Lock)
            {
                var redrawUIElementsEnabled = RedrawUIElementsEnabled;
                if (!preserveColors && SaveColors)
                {
                    BackupBackground();
                    BackupForeground();
                }

                if (s == null)
                {
                    if (DumpNullStringAsText != null)
                        ConsolePrint(DumpNullStringAsText, false);
                }
                else
                {
                    if (parseCommands)
                        ParseTextAndApplyCommands(s.ToString(), false, "", doNotEvalutatePrintDirectives, printSequences);
                    else
                        ConsolePrint(s.ToString(), false);
                }

                if (lineBreak) LineBreak();

                RedrawUIElementsEnabled = redrawUIElementsEnabled;
            }
        }

        void ParseTextAndApplyCommands(
            string s,
            bool lineBreak = false,
            string tmps = "",
            bool doNotEvalutatePrintDirectives = false,
            PrintSequences printSequences = null,
            int startIndex = 0)
        {
            lock (Lock)
            {
                int i = 0;
                KeyValuePair<string, CommandDelegate>? cmd = null;
                int n = s.Length;
                bool isAssignation = false;
                int cmdindex = -1;
                while (cmd == null && i < n)
                {
                    foreach (var ccmd in _drtvs)
                    {
                        if (s.IndexOf(CommandBlockBeginChar + ccmd.Key, i) == i)
                        {
                            cmd = ccmd;
                            cmdindex = i;
                            isAssignation = ccmd.Key.EndsWith("=");
                        }
                    }
                    if (cmd == null)
                        tmps += s.Substring(i, 1);
                    i++;
                }
                if (cmd == null)
                {
                    ConsolePrint(tmps, false);

                    printSequences?.Add(new PrintSequence((string)null, 0, i - 1, null, tmps, startIndex));
                    return;
                }
                else i = cmdindex;

                if (!string.IsNullOrEmpty(tmps))
                {
                    ConsolePrint(tmps);

                    printSequences?.Add(new PrintSequence((string)null, 0, i - 1, null, tmps, startIndex));
                }

                int firstCommandEndIndex = 0;
                int k = -1;
                string value = null;
                if (isAssignation)
                {
                    firstCommandEndIndex = s.IndexOf(CommandValueAssignationChar, i + 1);
                    if (firstCommandEndIndex > -1)
                    {
                        firstCommandEndIndex++;
                        var subs = s.Substring(firstCommandEndIndex);
                        if (subs.StartsWith(CodeBlockBegin))
                        {
                            firstCommandEndIndex += CodeBlockBegin.Length;
                            k = s.IndexOf(CodeBlockEnd, firstCommandEndIndex);
                            if (k > -1)
                            {
#pragma warning disable IDE0057
                                value = s.Substring(firstCommandEndIndex, k - firstCommandEndIndex);
#pragma warning restore IDE0057
                                k += CodeBlockEnd.Length;
                            }
                            else
                            {
                                ConsolePrint(s);

                                printSequences?.Add(new PrintSequence((string)null, i, s.Length - 1, null, s, startIndex));
                                return;
                            }
                        }
                    }
                }

                int j = i + cmd.Value.Key.Length;
                bool inCmt = false;
                int firstCommandSeparatorCharIndex = -1;
                while (j < s.Length)
                {
                    if (inCmt && s.IndexOf(CodeBlockEnd, j) == j)
                    {
                        inCmt = false;
                        j += CodeBlockEnd.Length - 1;
                    }
                    if (!inCmt && s.IndexOf(CodeBlockBegin, j) == j)
                    {
                        inCmt = true;
                        j += CodeBlockBegin.Length - 1;
                    }
                    if (!inCmt && s.IndexOf(CommandSeparatorChar, j) == j && firstCommandSeparatorCharIndex == -1)
                        firstCommandSeparatorCharIndex = j;
                    if (!inCmt && s.IndexOf(CommandBlockEndChar, j) == j)
                        break;
                    j++;
                }
                if (j == s.Length)
                {
                    ConsolePrint(s);

                    printSequences?.Add(new PrintSequence((string)null, i, j, null, s, startIndex));
                    return;
                }

                var cmdtxt = s[i..j];
                if (firstCommandSeparatorCharIndex > -1)
                    cmdtxt = cmdtxt.Substring(0, firstCommandSeparatorCharIndex - i/*-1*/);

                object result = null;
                if (isAssignation)
                {
                    if (value == null)
                    {
                        var t = cmdtxt.Split(CommandValueAssignationChar);
                        value = t[1];
                    }
                    if (!doNotEvalutatePrintDirectives) result = cmd.Value.Value(value);
                    
                    if (FileEchoEnabled && FileEchoCommands)
                        FileEcho(CommandBlockBeginChar + cmd.Value.Key + value + CommandBlockEndChar);

                    printSequences?.Add(new PrintSequence(cmd.Value.Key.Substring(0, cmd.Value.Key.Length - 1), i, j, value, null, startIndex));
                }
                else
                {
                    if (!doNotEvalutatePrintDirectives) result = cmd.Value.Value(null);
                    
                    if (FileEchoEnabled && FileEchoCommands)
                        FileEcho(CommandBlockBeginChar + cmd.Value.Key + CommandBlockEndChar);
                    
                    printSequences?.Add(new PrintSequence(cmd.Value.Key, i, j, value, null, startIndex));
                }
                if (result != null)
                    Print(result, false);

                if (firstCommandSeparatorCharIndex > -1)
                {
                    s = CommandBlockBeginChar + s.Substring(firstCommandSeparatorCharIndex + 1 /*+ i*/ );
                    startIndex += firstCommandSeparatorCharIndex + 1;
                }
                else
                {
                    if (j + 1 < s.Length)
                    {
                        s = s.Substring(j + 1);
                        startIndex += j + 1;
                    }
                    else
                        s = string.Empty;
                }

                if (!string.IsNullOrEmpty(s)) ParseTextAndApplyCommands(s, lineBreak, "", doNotEvalutatePrintDirectives, printSequences, startIndex);
            }
        }

        void ConsoleSubPrint(string s, bool lineBreak = false)
        {
            lock (Lock)
            {
                var (id, x, y, w, h) = ActualWorkArea();
                var x0 = CursorLeft;
                var y0 = CursorTop;
                if (EnableConstraintConsolePrintInsideWorkArea)
                {
                    var croppedLines = new List<string>();
                    var xr = x0 + s.Length - 1;
                    var xm = x + w - 1;
                    if (xr > xm)
                    {
                        while (xr > xm && s.Length > 0)
                        {
                            var left = s.Substring(0, s.Length - (xr - xm));
                            s = s.Substring(s.Length - (xr - xm), xr - xm);
                            croppedLines.Add(left);
                            xr = x + s.Length - 1;
                        }
                        if (s.Length > 0)
                            croppedLines.Add(s);
                        var curx = x0;
                        foreach (var line in croppedLines)
                        {
                            Write(line);
                            x0 += line.Length;
                            SetCursorPosConstraintedInWorkArea(ref x0, ref y0);
                            FileEcho(line);
                        }
                        if (lineBreak)
                        {
                            x0 = x;
                            y0++;
                            SetCursorPosConstraintedInWorkArea(ref x0, ref y0);
                        }
                    }
                    else
                    {
                        Write(s);
                        x0 += s.Length;
                        SetCursorPosConstraintedInWorkArea(ref x0, ref y0);
                        FileEcho(s);
                        if (lineBreak)
                        {
                            x0 = x;
                            y0++;
                            SetCursorPosConstraintedInWorkArea(ref x0, ref y0);
                        }
                    }
                }
                else
                {
                    var dep = CursorLeft + s.Length - 1 > x + w - 1;
                    if (dep)
                    {
                        Write(s);
                        // removed: too slow & buggy (s.Length is wrong due to ansi codes)
                        //if (!IsRedirected) FillLineFromCursor(' ');   // this fix avoid background color to fill the full line on wsl/linux
                    }
                    else
                        Write(s);
                    FileEcho(s);
                    if (lineBreak)
                    {
                        var f = _textWriter.ForegroundColor;
                        var b = _textWriter.BackgroundColor;
                        if (!IsRedirected)
                        {
                            _textWriter.ForegroundColor = ColorSettings.Default.Foreground.Value;
                            _textWriter.BackgroundColor = ColorSettings.Default.Background.Value;
                            _textWriter.WriteLine(string.Empty);
                        }
                        FileEcho(string.Empty, true);
                        if (!IsRedirected)
                        {
                            _textWriter.ForegroundColor = f;
                            _textWriter.BackgroundColor = b;
                        }
                    }
                }
            }
        }

        void FillLineFromCursor(char c = ' ', bool resetCursorLeft = true, bool useDefaultColors = true)
        {
            lock (Lock)
            {
                if (!EnableFillLineFromCursor) return;
                var f = _textWriter.ForegroundColor;
                var b = _textWriter.BackgroundColor;
                var aw = ActualWorkArea();
                var nb = Math.Max(0, Math.Max(aw.Right, _textWriter.BufferWidth - 1) - CursorLeft - 1);
                var x = CursorLeft;
                var y = CursorTop;
                if (useDefaultColors)
                {
                    _textWriter.ForegroundColor = ColorSettings.Default.Foreground.Value;
                    _textWriter.BackgroundColor = ConsoleColor.Red; // ColorSettings.Default.Background.Value;
                }
                Write("".PadLeft(nb, c));   // TODO: BUG in WINDOWS: do not print the last character
                SetCursorPos(nb, y);
                Write(" ");
                if (useDefaultColors)
                {
                    _textWriter.ForegroundColor = f;
                    _textWriter.BackgroundColor = b;
                }
                if (resetCursorLeft)
                    _textWriter.CursorLeft = x;
            }
        }

        public int GetIndexInWorkAreaConstraintedString(
            string s,
            Point origin,
            Point cursorPos,
            bool forceEnableConstraintInWorkArea = false,
            bool fitToVisibleArea = true,
            bool doNotEvaluatePrintDirectives = true,
            bool ignorePrintDirectives = false
            )
            => GetIndexInWorkAreaConstraintedString(
                s,
                origin,
                cursorPos.X,
                cursorPos.Y,
                forceEnableConstraintInWorkArea,
                fitToVisibleArea,
                doNotEvaluatePrintDirectives,
                ignorePrintDirectives);

        public int GetIndexInWorkAreaConstraintedString(
            string s,
            Point origin,
            int cursorX,
            int cursorY,
            bool forceEnableConstraintInWorkArea = false,
            bool fitToVisibleArea = true,
            bool doNotEvaluatePrintDirectives = true,
            bool ignorePrintDirectives = false)
        {
            var r = GetWorkAreaStringSplits(
                s,
                origin,
                forceEnableConstraintInWorkArea,
                fitToVisibleArea,
                doNotEvaluatePrintDirectives,
                ignorePrintDirectives,
                cursorX,
                cursorY
                );
            return r.CursorIndex;
        }

        public LineSplits GetIndexLineSplitsInWorkAreaConstraintedString(
            string s,
            Point origin,
            int cursorX,
            int cursorY,
            bool forceEnableConstraintInWorkArea = false,
            bool fitToVisibleArea = true,
            bool doNotEvaluatePrintDirectives = false,
            bool ignorePrintDirectives = false)
        {
            var r = GetWorkAreaStringSplits(
                s,
                origin,
                forceEnableConstraintInWorkArea,
                fitToVisibleArea,
                doNotEvaluatePrintDirectives,
                ignorePrintDirectives,
                cursorX,
                cursorY
                );
            return r;
        }

        public LineSplits GetWorkAreaStringSplits(
            string s,
            Point origin,
            bool forceEnableConstraintInWorkArea = false,
            bool fitToVisibleArea = true,
            bool doNotEvaluatePrintDirectives = false,
            bool ignorePrintDirectives = false,
            int cursorX = -1,
            int cursorY = -1)
        {
            var originalString = s;
            var r = new List<StringSegment>();
            PrintSequences printSequences = null;
            if (cursorX == -1) cursorX = origin.X;
            if (cursorY == -1) cursorY = origin.Y;
            int cursorLineIndex = -1;
            int cursorIndex = -1;

            lock (Lock)
            {
                int index = -1;
                var (id, x, y, w, h) = ActualWorkArea(fitToVisibleArea);
                var x0 = origin.X;
                var y0 = origin.Y;

                var croppedLines = new List<StringSegment>();
                string pds = null;
                var length = s.Length;
                if (doNotEvaluatePrintDirectives)
                {
                    pds = s;
                    printSequences = new PrintSequences();
                    s = GetPrint(s, false, doNotEvaluatePrintDirectives, ignorePrintDirectives, printSequences);
                }
                var xr = x0 + s.Length - 1;
                var xm = x + w - 1;

                if (xr >= xm)
                {
                    if (pds != null)
                    {
                        var lineSegments = new List<string>();
                        var currentLine = string.Empty;
                        int lastIndex = 0;

                        foreach (var ps in printSequences)
                        {
                            if (!ps.IsText)
                                lineSegments.Add(ps.ToText());
                            else
                            {
                                currentLine += ps.Text;
                                xr = x0 + currentLine.Length - 1;
                                if (xr > xm && currentLine.Length > 0)
                                {
                                    while (xr > xm && currentLine.Length > 0)
                                    {
                                        var left = currentLine.Substring(0, currentLine.Length - (xr - xm));
                                        currentLine = currentLine.Substring(currentLine.Length - (xr - xm), xr - xm);

                                        var truncLeft = left.Substring(lastIndex);
                                        lineSegments.Add(truncLeft);
                                        croppedLines.Add(new StringSegment(string.Join("", lineSegments), 0, 0, lastIndex + truncLeft.Length));
                                        lineSegments.Clear();
                                        lastIndex = 0;

                                        xr = x + currentLine.Length - 1;
                                    }
                                    if (currentLine.Length > 0)
                                    {
                                        lineSegments.Add(currentLine);
                                        lastIndex = currentLine.Length;
                                    }
                                }
                                else
                                {
                                    lineSegments.Add(currentLine.Substring(lastIndex));
                                    lastIndex = currentLine.Length;
                                }
                            }
                        }

                        if (lineSegments.Count > 0)
                        {
                            var truncLeft = currentLine.Substring(lastIndex);
                            lineSegments.Add(truncLeft);
                            croppedLines.Add(new StringSegment(string.Join("", lineSegments), 0, 0, lastIndex + truncLeft.Length));
                            lineSegments.Clear();
                            lastIndex = 0;
                        }
                    }
                    else
                    {
                        while (xr > xm && s.Length > 0)
                        {
                            var left = s.Substring(0, s.Length - (xr - xm));
                            s = s.Substring(s.Length - (xr - xm), xr - xm);
                            croppedLines.Add(new StringSegment(left, 0, 0, left.Length));
                            xr = x + s.Length - 1;
                        }
                        if (s.Length > 0)
                            croppedLines.Add(new StringSegment(s, 0, 0, s.Length));
                    }

                    var curx = x0;
                    int lineIndex = 0;
                    index = 0;
                    bool indexFounds = false;
                    foreach (var line in croppedLines)
                    {
                        r.Add(new StringSegment(line.Text, x0, y0, line.Length));
                        if (!indexFounds && cursorY == y0)
                        {
                            index += cursorX - x0;
                            cursorIndex = index;
                            cursorLineIndex = lineIndex;
                            indexFounds = true;
                        }
                        x0 += line.Length;
                        index += line.Length;
                        SetCursorPosConstraintedInWorkArea(ref x0, ref y0, false, forceEnableConstraintInWorkArea, fitToVisibleArea);
                        lineIndex++;
                    }
                    if (!indexFounds)
                    {
                        cursorIndex = index;
                        cursorLineIndex = lineIndex;
                    }
                }
                else
                {
                    cursorIndex = cursorX - x0;
                    cursorLineIndex = 0;
                    if (pds != null)
                        r.Add(new StringSegment(pds, x0, y0, pds.Length));
                    else
                        r.Add(new StringSegment(s, x0, y0, s.Length));
                }
            }

            if (!doNotEvaluatePrintDirectives)
            {
                printSequences = new PrintSequences();
                printSequences.Add(new PrintSequence((string)null, 0, originalString.Length - 1, null, originalString));
            }

            return new LineSplits(r, printSequences, cursorIndex, cursorLineIndex);
        }

        public void SetCursorPosConstraintedInWorkArea(Point pos, bool enableOutput = true, bool forceEnableConstraintInWorkArea = false, bool fitToVisibleArea = true)
        {
            var x = pos.X;
            var y = pos.Y;
            SetCursorPosConstraintedInWorkArea(ref x, ref y, enableOutput, forceEnableConstraintInWorkArea, fitToVisibleArea);
        }

        public void SetCursorPosConstraintedInWorkArea(int cx, int cy, bool enableOutput = true, bool forceEnableConstraintInWorkArea = false, bool fitToVisibleArea = true)
            => SetCursorPosConstraintedInWorkArea(ref cx, ref cy, enableOutput, forceEnableConstraintInWorkArea, fitToVisibleArea);

        public void SetCursorPosConstraintedInWorkArea(ref int cx, ref int cy, bool enableOutput = true, bool forceEnableConstraintInWorkArea = false, bool fitToVisibleArea = true)
        {
            lock (Lock)
            {
                int dx = 0;
                int dy = 0;

                if (EnableConstraintConsolePrintInsideWorkArea || forceEnableConstraintInWorkArea)
                {
                    var (id, left, top, right, bottom) = ActualWorkArea(fitToVisibleArea);
                    if (cx < left)
                    {
                        cx = right - 1;
                        cy--;
                    }
                    if (cx >= right)
                    {
                        cx = left;
                        cy++;
                    }

                    if (enableOutput && cy < top)
                    {
                        dy = top - cy;
                        cy += dy;
                        if (top + 1 <= bottom)
                            _textWriter.MoveBufferArea(      // TODO: not supported on linux (ubuntu 18.04 wsl)
                                left, top, right, bottom - top,
                                left, top + 1,
                                ' ',
                                DefaultForeground, DefaultBackground);
                    }

                    if (enableOutput && cy > bottom /*- 1*/)
                    {
                        dy = bottom /*- 1*/ - cy;
                        cy += dy;
                        var nh = bottom - top + dy + 1;
                        if (nh > 0)
                        {
                            _textWriter.MoveBufferArea(      // TODO: not supported on linux (ubuntu 18.04 wsl)
                                left, top - dy, right, nh,
                                left, top,
                                ' ',
                                DefaultForeground, DefaultBackground);
                        }
                    }
                }

                if (enableOutput)
                {
                    SetCursorPos(cx, cy);
                    if (dx != 0 || dy != 0)
                        WorkAreaScrolled?.Invoke(null, new WorkAreaScrollEventArgs(0, dy));
                }
            }
        }

        #endregion

    }
}
