﻿using DotNetConsoleAppToolkit.Component.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.CompilerServices;
using static DotNetConsoleAppToolkit.DotNetConsole;
using cons= DotNetConsoleAppToolkit.DotNetConsole;
using static DotNetConsoleAppToolkit.Lib.Str;
using static DotNetConsoleAppToolkit.Component.UI.UIElement;
using sc = System.Console;
using static DotNetConsoleAppToolkit.Console.ANSISequences;

namespace DotNetConsoleAppToolkit.Console
{
    public class ConsoleTextWriterWrapper : TextWriterWrapper
    {
        #region attributes
        
        public bool RedirecToErr = false;

        #region console output settings

        public int CropX = -1;
        public bool EnableFillLineFromCursor = true;

        protected int _cursorLeftBackup;
        protected int _cursorTopBackup;
        protected ConsoleColor _backgroundBackup = ConsoleColor.Black;
        protected ConsoleColor _foregroundBackup = ConsoleColor.White;
        protected Dictionary<string, CommandDelegate> _drtvs;

        public static readonly string Esc = (char)27+"";
        
        #region console information cache

        protected Point _cachedCursorPosition = Point.Empty;
        protected Size _cachedBufferSize = Size.Empty;
        ConsoleColor _cachedForegroundColor;
        ConsoleColor _cachedBackgroundColor;

        #endregion

        #endregion

        #endregion

        #region construction & init

        public ConsoleTextWriterWrapper() : base() { Init(); }

        public ConsoleTextWriterWrapper(TextWriter textWriter) : base(textWriter) { Init(); }

        void Init()
        {
            _cachedForegroundColor = sc.ForegroundColor;
            _cachedBackgroundColor = sc.BackgroundColor;
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
                { PrintDirectives.crx+"="  , (x) => RelayCall(() => CursorLeft = GetCursorX(x)) },
                { PrintDirectives.cry+"="  , (x) => RelayCall(() => CursorTop = GetCursorY(x)) },
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

        void BackupCursorInformation()
        {
            _cachedCursorPosition = CursorPos;
            _cachedBufferSize = new Size(sc.BufferWidth, sc.BufferHeight);
        }

        void ClearCursorInformation()
        {
            _cachedCursorPosition = Point.Empty;
            _cachedBufferSize = Size.Empty;
        }

        public override void EnableBuffer()
        {
            lock (Lock)
            {
                if (!IsBufferEnabled)
                {
                    base.EnableBuffer();
                    BackupCursorInformation();
                }
            }
        }

        public override void CloseBuffer()
        {
            lock (Lock)
            {
                if (IsBufferEnabled)
                {
                    base.CloseBuffer();
                    ClearCursorInformation();
                }
            }
        }

        #endregion

        #region console output operations

        protected delegate object CommandDelegate(object x);
        
        object RelayCall(Action method) { method(); return null; }

        public void CursorHome() => Locked(() => { Write($"{(char)27}[H"); });
        
        public void ClearLineFromCursorRight() => Locked(() => { Write($"{(char)27}[K"); });
        
        public void ClearLineFromCursorLeft() => Locked(() => { Write($"{(char)27}[1K"); });
        
        public void ClearLine() => Locked(() => { Write($"{(char)27}[2K"); });

        public void FillFromCursorRight()
        {
            lock (Lock)
            {
                FillLineFromCursor(' ', false, false);
            }
        }

        public void EnableInvert() => Locked(() => { Write($"{(char)27}[7m"); });
        
        public void EnableBlink() => Locked(() => { Write($"{(char)27}[5m"); });           // not available on many consoles
        
        public void EnableLowIntensity() => Locked(() => { Write($"{(char)27}[2m"); });    // not available on many consoles
        
        public void EnableUnderline() => Locked(() => { Write($"{(char)27}[4m"); });
        
        public void EnableBold() => Locked(() => { Write($"{(char)27}[1m"); });            // not available on many consoles
        
        public void DisableTextDecoration() => Locked(() => { Write($"{(char)27}[0m"); RestoreDefaultColors(); });

        public void MoveCursorDown(int n = 1) => Locked(() => { Write($"{(char)27}[{n}B"); });

        public void MoveCursorTop(int n = 1) => Locked(() => { Write($"{(char)27}[{n}A"); });

        public void MoveCursorLeft(int n = 1) => Locked(() => { Write($"{(char)27}[{n}D"); });

        public void MoveCursorRight(int n = 1) => Locked(() => { Write($"{(char)27}[{n}C"); });

        public void ScrollWindowDown(int n = 1) { Write(((char)27) + $"[{n}T"); }

        public void ScrollWindowUp(int n = 1) { Write(((char)27) + $"[{n}S"); }

        /// <summary>
        /// backup the current 3bit foreground color
        /// </summary>
        public void BackupForeground() => Locked(() =>
        {
            if (IsBufferEnabled) throw new BufferedOperationNotAvailableException();
            _foregroundBackup = sc.ForegroundColor;
        });

        /// <summary>
        /// backup the current 3bit background color
        /// </summary>
        public void BackupBackground() => Locked(() => {
            if (IsBufferEnabled) throw new BufferedOperationNotAvailableException();
            _backgroundBackup = sc.BackgroundColor;
        });

        public void RestoreForeground() => Locked(() => SetForeground( _foregroundBackup ));

        public void RestoreBackground() => Locked(() => SetBackground( _backgroundBackup ));

        /// <summary>
        /// set foreground color from a 3 bit palette color (ConsoleColor to ansi)
        /// </summary>
        /// <param name="c"></param>
        public void SetForeground(ConsoleColor c)
        {
            lock (Lock)
            {
                if (Enum.TryParse<Color3BitToAnsi>((c + "").ToLower(), out var colbit))
                {
                    var num = (int)colbit & 0b111;
                    var isDark = ((int)colbit & 0b1000) != 0;
                    //WriteLine($"num={num} isDark={isDark}");
                    //sc.ForegroundColor = c;
                    Write(((char)27)+"["+(!isDark?$"1;3{num,1}m": $"3{num,1}m"));
                }
            }
        }

        public void SetBackground(ConsoleColor c)
        {
            lock (Lock)
            {
                if (Enum.TryParse<Color3BitToAnsi>((c + "").ToLower(), out var colbit))
                {
                    var num = (int)colbit & 0X111;
                    var isDark = ((int)colbit & 0X1000) != 0;
                    //sc.ForegroundColor = c;
                    Write(((char)27) + "[" + (!isDark ? $"1;4{num,1}m" : $"4{num,1}m"));
                }
            }
            //_textWriter.BackgroundColor = c;
        }

        /// <summary>
        /// set foreground color from a 8 bit palette color (vt/ansi)
        /// </summary>
        /// <param name="c"></param>
        public void SetForeground(int c) => Locked(() =>
        {
            Write($"{(char)27}[38;5;{c}m");
        });

        /// <summary>
        /// set background color from a 8 bit palette color (vt/ansi)
        /// </summary>
        /// <param name="c"></param>
        public void SetBackground(int c) => Locked(() =>
        {
            Write($"{(char)27}[48;5;{c}m");
        });

        /// <summary>
        /// set foreground color from a 24 bit palette color (vt/ansi)
        /// </summary>
        /// <param name="r">red from 0 to 255</param>
        /// <param name="g">green from 0 to 255</param>
        /// <param name="b">blue from 0 to 255</param>
        public void SetForeground(int r, int g, int b) => Locked(() =>
        {
            Write($"{(char)27}[38;2;{r};{g};{b}m");
        });

        /// <summary>
        /// set background color from a 24 bit palette color (vt/ansi)
        /// </summary>
        /// <param name="r">red from 0 to 255</param>
        /// <param name="g">green from 0 to 255</param>
        /// <param name="b">blue from 0 to 255</param>
        public void SetBackground(int r, int g, int b) => Locked(() =>
        {
            Write($"{(char)27}[48;2;{r};{g};{b}m");
        });

        public void SetForeground((int r, int g, int b) color) => SetForeground(color.r, color.g, color.b);
        
        public void SetBackground((int r, int g, int b) color) => SetBackground(color.r, color.g, color.b);

        public void SetDefaultForeground(ConsoleColor c) => Locked(() => DefaultForeground = c);
        
        public void SetDefaultBackground(ConsoleColor c) => Locked(() => DefaultBackground = c);

        public void SetDefaultColors(ConsoleColor foregroundColor, ConsoleColor backgroundColor) => 
            Locked(() => {
                SetDefaultForeground( foregroundColor );
                SetDefaultForeground( backgroundColor );
            });

        public void RestoreDefaultColors() => Locked(() => { 
            SetForeground( DefaultForeground); 
            SetBackground( DefaultBackground); 
        });
        
        public void ClearScreen()
        {
            Locked(() =>
            {
                if (IsBufferEnabled) throw new BufferedOperationNotAvailableException();
                RestoreDefaultColors();
                sc.Clear();
                //Write(Esc+"[2J" + Esc + "[0;0H"); // bugged on windows
                UpdateUI(true, false);
            });
        }
        
        public void LineBreak()
        {
            Locked(() =>
            {
                //ConsolePrint(string.Empty, true);
                Write(LNBRK);
            });
        }

        public void BackupCursorPos()
        {
            Locked(() =>
            {
                _cursorLeftBackup = CursorLeft;
                _cursorTopBackup = CursorTop;
            });
        }
        
        public void RestoreCursorPos()
        {
            Locked(() =>
            {
                Write(Esc + "[2J" + Esc + $"[{_cursorTopBackup+1};{_cursorLeftBackup+1}H");
                //_textWriter.CursorLeft = _cursorLeftBackup;
                //textWriter.CursorTop = _cursorTopBackup;
            });
        }
        
        //public void SetCursorLeft(int x) => Locked(() => _textWriter.CursorLeft = FixX(x));
        
        //public void SetCursorTop(int y) => Locked(() => _textWriter.CursorTop = FixY(y));
        
        /// <summary>
        /// get/set cursor column
        /// </summary>
        public int CursorLeft
        {
            get { 
                lock (Lock) { 
                    return IsBufferEnabled? _cachedCursorPosition.X : sc.CursorLeft; 
                } 
            }
            set {  
                lock (Lock)
                {
                    _cachedCursorPosition.X = value;
                    Write(Esc + "["+value+"G");
                } 
            }
        }
        
        /// <summary>
        /// get/set cursor top
        /// </summary>
        public int CursorTop
        {
            get
            {
                lock (Lock)
                {
                    return IsBufferEnabled ? _cachedCursorPosition.X : sc.CursorTop;
                }
            }
            set
            {
                lock (Lock)
                {
                    _cachedCursorPosition.Y = value;
                    Write(Esc + "[2J" + Esc + $"[{value+1};{CursorLeft+1}H");
                }
            }
        }
        
        public Point CursorPos
        {
            get
            {
                lock (Lock)
                { 
                    return new Point(CursorLeft, CursorTop); 
                }
            }
        }
        
        public void SetCursorPos(Point p)
        {
            lock (Lock)
            {
                var x = p.X;
                var y = p.Y;
                FixCoords(ref x, ref y);
                if (IsBufferEnabled)
                {
                    _cachedCursorPosition.X = x;
                    _cachedCursorPosition.Y = y;
                }
                //_textWriter.CursorLeft = x;
                //_textWriter.CursorTop = y;
                Write(Esc + $"[{y+1};{x+1}H");
            }
        }
        
        public void SetCursorPos(int x, int y)
        {
            lock (Lock)
            {
                FixCoords(ref x, ref y);
                if (IsBufferEnabled)
                {
                    _cachedCursorPosition.X = x;
                    _cachedCursorPosition.Y = y;
                }
                Write(Esc + $"[{(y+1)};{(x+1)}H");
            }
        }
        
        public bool CursorVisible => sc.CursorVisible;
        
        public void HideCur() => Locked(() => sc.CursorVisible = false);
        
        public void ShowCur() => Locked(() => sc.CursorVisible = true);              

        public string GetPrint(
            string s,
            bool lineBreak = false,
            bool doNotEvaluatePrintDirectives = false,      // TODO: remove this parameter
            bool ignorePrintDirectives = false,
            PrintSequences printSequences = null)
        {
            lock (Lock)
            {
                if (string.IsNullOrWhiteSpace(s)) return s;
                var ms = new MemoryStream(s.Length * 4);
                var sw = new StreamWriter(ms);
                RedirectOut(sw);
                var e = EnableConstraintConsolePrintInsideWorkArea;
                EnableConstraintConsolePrintInsideWorkArea = false;
                Print(s, lineBreak, false, !ignorePrintDirectives, true, printSequences);
                EnableConstraintConsolePrintInsideWorkArea = e;
                sw.Flush();
                ms.Position = 0;
                var rw = new StreamReader(ms);
                var txt = rw.ReadToEnd();
                rw.Close();
                RedirectOut((StreamWriter)null);
                return txt;
            }
        }

        public string GetPrintWithEscapeSequences(string s, bool lineBreak = false)
        {
            lock (Lock)
            {
                if (string.IsNullOrWhiteSpace(s)) return s;
                var ms = new MemoryStream(s.Length * 4);
                var sw = new StreamWriter(ms);
                RedirectOut(sw);
                var e = EnableConstraintConsolePrintInsideWorkArea;
                EnableConstraintConsolePrintInsideWorkArea = false;
                Print(s, lineBreak);
                EnableConstraintConsolePrintInsideWorkArea = e;
                sw.Flush();
                ms.Position = 0;
                var rw = new StreamReader(ms);
                var txt = rw.ReadToEnd();
                rw.Close();
                RedirectOut((StreamWriter)null);
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
            {
                if (IsBufferEnabled)
                    _echoStreamWriter?.Write($"x={CursorLeft},y={CursorTop},l={s.Length}, bw={_cachedBufferSize},bh={_cachedBufferSize},br={lineBreak} [{callerMemberName}:{callerLineNumber}] :");
                else
                    _echoStreamWriter?.Write($"x={CursorLeft},y={CursorTop},l={s.Length},w={sc.WindowWidth},h={sc.WindowHeight},wtop={sc.WindowTop} bw={sc.BufferWidth},bh={sc.BufferHeight},br={lineBreak} [{callerMemberName}:{callerLineNumber}] :");
            }
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

        public void Println(IEnumerable<string> ls, bool ignorePrintDirectives = false) { foreach (var s in ls) Println(s, ignorePrintDirectives); }
        
        public void Print(IEnumerable<string> ls, bool lineBreak = false, bool ignorePrintDirectives = false) { foreach (var s in ls) Print(s, lineBreak, ignorePrintDirectives); }
        
        public void Println(string s = "", bool ignorePrintDirectives = false) => Print(s, true, false, !ignorePrintDirectives);
        
        public void Print(string s = "", bool lineBreak = false, bool ignorePrintDirectives = false) => Print(s, lineBreak, false, !ignorePrintDirectives);
        
        public void Println(char s, bool ignorePrintDirectives = false) => Print(s + "", true, false, !ignorePrintDirectives);
        
        public void Print(char s, bool lineBreak = false, bool ignorePrintDirectives = false) => Print(s + "", lineBreak, !ignorePrintDirectives);

        public void Print(
            object s,
            bool lineBreak = false,
            bool preserveColors = false,        // TODO: remove this parameter + SaveColors property
            bool parseCommands = true,
            bool doNotEvalutatePrintDirectives = false,
            PrintSequences printSequences = null)
        {
            lock (Lock)
            {
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
                        var f = _cachedForegroundColor;
                        var b = _cachedBackgroundColor;
                        if (!IsRedirected)
                        {
                            SetForeground( ColorSettings.Default.Foreground.Value );
                            SetBackground( ColorSettings.Default.Background.Value );
                            _textWriter.WriteLine(string.Empty);
                        }

                        FileEcho(string.Empty, true);

                        if (!IsRedirected)
                        {
                            SetForeground( f );
                            SetBackground( b );
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
                var f = _cachedForegroundColor;
                var b = _cachedForegroundColor;
                var aw = ActualWorkArea();
                var nb = Math.Max(0, Math.Max(aw.Right, _cachedBufferSize.Width - 1) - CursorLeft - 1);
                var x = CursorLeft;
                var y = CursorTop;
                if (useDefaultColors)
                {
                    SetForeground( ColorSettings.Default.Foreground.Value );
                    SetBackground( ColorSettings.Default.Background.Value );
                }
                Write("".PadLeft(nb, c));   // TODO: BUG in WINDOWS: do not print the last character
                SetCursorPos(nb, y);
                Write(" ");
                if (useDefaultColors)
                {
                    SetForeground( f );
                    SetBackground( b );
                }
                if (resetCursorLeft)
                    CursorLeft = x;
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

        /// <summary>
        /// TODO: check for buffered mode
        /// </summary>
        /// <param name="s"></param>
        /// <param name="origin"></param>
        /// <param name="forceEnableConstraintInWorkArea"></param>
        /// <param name="fitToVisibleArea"></param>
        /// <param name="doNotEvaluatePrintDirectives"></param>
        /// <param name="ignorePrintDirectives"></param>
        /// <param name="cursorX"></param>
        /// <param name="cursorY"></param>
        /// <returns></returns>
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

        /// <summary>
        /// TODO: check for buffered mode
        /// </summary>
        /// <param name="cx"></param>
        /// <param name="cy"></param>
        /// <param name="enableOutput"></param>
        /// <param name="forceEnableConstraintInWorkArea"></param>
        /// <param name="fitToVisibleArea"></param>
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
                            sc.MoveBufferArea(      // TODO: not supported on linux (ubuntu 18.04 wsl)
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
                            sc.MoveBufferArea(      // TODO: not supported on linux (ubuntu 18.04 wsl)
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
