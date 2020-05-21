#define dbg

using DotNetConsoleSdk.Component;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using static DotNetConsoleSdk.Component.UIElement;
using sc = System.Console;

namespace DotNetConsoleSdk
{
    /// <summary>
    /// dotnet core sdk helps build fastly nice console applications
    /// </summary>
    public static class DotNetConsoleSdk
    {
        #region attributes

        public static bool ClearOnViewResized = true;
        public static bool SaveColors = true;
        public static bool TraceCommandErrors = true;
        public static bool EnableColors = true;
        public static bool DumpExceptions = false;
        public static ConsoleColor DefaultForeground = ConsoleColor.White;
        public static ConsoleColor DefaultBackground = ConsoleColor.Black;
        public static char CommandBlockBeginChar = '(';
        public static char CommandBlockEndChar = ')';
        public static char CommandSeparatorChar = ',';
        public static char CommandValueAssignationChar = '=';
        public static string DumpNullStringAsText = "{null}";
        public static string CodeBlockBegin = "[[!--";
        public static string CodeBlockEnd = "--]]";
        
        static int _cursorLeftBackup;
        static int _cursorTopBackup;
        static ConsoleColor _backgroundBackup = ConsoleColor.Black;
        static ConsoleColor _foregroundBackup = ConsoleColor.White;
        static readonly string[] _crlf = { Environment.NewLine };
        static Rectangle _workArea = Rectangle.Empty;

        static Thread _watcherThread;
        static Dictionary<int, UIElement> _uielements = new Dictionary<int, UIElement>();

        static string[] _args;
        static TextWriter _outputWriter;
        static StreamWriter _outputStreamWriter;
        static FileStream _outputFileStream;
        static StreamWriter _echoStreamWriter;
        static FileStream _echoFileStream;

        static Dictionary<string, Script<object>> _csscripts = new Dictionary<string, Script<object>>();

        public static object ConsoleLock = new object();
        static Thread _readerThread;

        static List<string> _history = new List<string>();
        static int _historyIndex = -1;

        #endregion

        #region log methods

        public static void LogError(Exception ex)
        {
            if (DumpExceptions)
                LogException(ex);
            else
            {
                var msg = ex.Message;
                while (ex.InnerException != null)
                {
                    ex = ex.InnerException;
                    msg += Environment.NewLine + ex.Message;
                }
                var ls = msg.Split(_crlf, StringSplitOptions.None)
                    .Select(x => ((EnableColors) ? $"{Red}" : "") + x);
                Println(ls);
            }
        }

        public static void LogException(Exception ex)
        {
            var ls = (ex + "").Split(_crlf, StringSplitOptions.None)
                .Select(x => ((EnableColors) ? $"{Red}" : "") + x);
            Println(ls);
        }

        public static void LogError(string s)
        {
            var ls = (s + "").Split(_crlf, StringSplitOptions.None)
                .Select(x => ((EnableColors) ? $"{Red}" : "") + x);
            Println(ls);
        }

        public static void LogWarning(string s)
        {
            var ls = (s + "").Split(_crlf, StringSplitOptions.None)
                .Select(x => ((EnableColors) ? $"{Yellow}" : "") + x);
            Println(ls);
        }

        public static void Log(string s)
        {
            Println(s.Split(_crlf, StringSplitOptions.None));
        }

        #endregion

        #region console operations

        /*
         * global syntax:
         *      commandBlockBegin command commandValueAssignationChar value (commandSeparatorChar command commandValueAssignationChar value)* commandBlockEnd
         *      commandBlockBegin := (
         *      commandBlockEnd := )
         *      commandValueAssignationChar := =
         *      commandSeparatorChar := ,
         *      value := string_without_CommandBlockBegin_and_CommandBlockEnd) | ( codeBlockBegin any codeBlockEnd )
         *      any := string
         *      codeBlockBegin ::= [[!--
         *      codeBlockEnd ::= --]]
         * colors: 
         *      set foreground:     f=consoleColor
         *      set background:     b=consoleColor
         *      set default foreground: df=consoleColor
         *      set default background: db=consoleColor
         *      backup foreground:  bkf
         *      backup background:  bkb
         *      restore foreground: rsf
         *      restore background: rsb
         *      consoleColor (ignoreCase) := Black | DarkBlue | DarkGreen | DarkCyan | DarkRed  | DarkMagenta | DarkYellow | Gray | DarkGray  | Blue | Green | Cyan  | Red  | Magenta  | Yellow  | White
         * print control:
         *      clear console: cl
         *      line break: br
         *      backup cursor pos: bkcr
         *      restore cursor pos: rscr
         *      set cursor left: crl=
         *      set cursor top: crt=
         * app control:
         *      exit: exit
         * scripts engines:
         *      exec: exec csharp from text
         */

        public enum KeyWords
        {
            bkf,
            bkb,
            rsf,
            rsb,
            cl,
            f,
            b,
            df,
            db,
            br,
            inf,
            bkcr,
            rscr,
            crh,
            crs,
            crx,
            cry,
            exit,
            exec
        }

        delegate object CommandDelegate(object x);

        static readonly Dictionary<string, CommandDelegate> _drtvs = new Dictionary<string, CommandDelegate>() {
            { KeyWords.bkf+""   , (x) => RelayCall(BackupForeground) },
            { KeyWords.bkb+""   , (x) => RelayCall(BackupBackground) },
            { KeyWords.rsf+""   , (x) => RelayCall(RestoreForeground) },
            { KeyWords.rsb+""   , (x) => RelayCall(RestoreBackground) },
            { KeyWords.cl+""    , (x) => RelayCall(() => Clear()) },
            { KeyWords.f+"="    , (x) => RelayCall(() => SetForeground( ParseColor(x))) },
            { KeyWords.b+"="    , (x) => RelayCall(() => SetBackground( ParseColor(x))) },
            { KeyWords.df+"="   , (x) => RelayCall(() => SetDefaultForeground( ParseColor(x))) },
            { KeyWords.db+"="   , (x) => RelayCall(() => SetDefaultBackground( ParseColor(x))) },
            { KeyWords.br+""    , (x) => RelayCall(LineBreak) },
            { KeyWords.inf+""   , (x) => RelayCall(Infos) },
            { KeyWords.bkcr+""  , (x) => RelayCall(BackupCursorPos) },
            { KeyWords.rscr+""  , (x) => RelayCall(RestoreCursorPos) },
            { KeyWords.crh+""   , (x) => RelayCall(HideCur) },
            { KeyWords.crs+""   , (x) => RelayCall(ShowCur) },
            { KeyWords.crx+"="  , (x) => RelayCall(() => SetCursorLeft(GetCursorX(x))) },
            { KeyWords.cry+"="  , (x) => RelayCall(() => SetCursorTop(GetCursorY(x))) },
            { KeyWords.exit+""  , (x) => RelayCall(() => Exit()) },
            { KeyWords.exec+"=" , (x) => ExecCSharp((string)x) }
        };

        static object RelayCall(Action method) { method(); return null; }
        public static void Lock(Action action)
        {
            lock (ConsoleLock)
            {
                action?.Invoke();
            }
        }

        public static void BackupForeground() => Lock(()=>_foregroundBackup = sc.ForegroundColor);
        public static void BackupBackground() => Lock(() => _backgroundBackup = sc.BackgroundColor);
        public static void RestoreForeground() => Lock(() => sc.ForegroundColor = _foregroundBackup);
        public static void RestoreBackground() => Lock(() => sc.BackgroundColor = _backgroundBackup);
        public static void SetForeground(ConsoleColor c) => Lock(() => sc.ForegroundColor = c);
        public static void SetBackground(ConsoleColor c) => Lock(() => sc.BackgroundColor = c);
        public static void SetDefaultForeground(ConsoleColor c) => Lock(() => DefaultForeground = c);
        public static void SetDefaultBackground(ConsoleColor c) => Lock(() => DefaultBackground = c);
        public static void Clear()
        {
            Lock(() =>
            {
                sc.Clear();
                RedrawUI();
            }
            );
        }
        public static void LineBreak()
        {
            Lock(() =>
            {
                ConsolePrint(string.Empty, true);
                RedrawUI();
            });
        }
        public static void Infos()
        {
            Lock(() =>
            {
                Println($"{White}{Bkf}{Green}window:{Rf} left={Cyan}{sc.WindowLeft}{Rf},top={Cyan}{sc.WindowTop}{Rf},width={Cyan}{sc.WindowWidth}{Rf},height={Cyan}{sc.WindowHeight}{Rf},largest width={Cyan}{sc.LargestWindowWidth}{Rf},largest height={Cyan}{sc.LargestWindowHeight}{Rf}");
                Println($"{Green}buffer:{Rf} width={Cyan}{sc.BufferWidth}{Rf},height={Cyan}{sc.BufferHeight}{Rf} | input encoding={Cyan}{sc.InputEncoding.EncodingName}{Rf} | output encoding={Cyan}{sc.OutputEncoding.EncodingName}{Rf}");
                Println($"number lock={Cyan}{sc.NumberLock}{Rf} | capslock={Cyan}{sc.CapsLock}{Rf} | cursor visible={Cyan}{sc.CursorVisible}{Rf} | cursor size={Cyan}{sc.CursorSize}");
            });
        }
        public static void BackupCursorPos()
        {
            Lock(() =>
            {
                _cursorLeftBackup = sc.CursorLeft;
                _cursorTopBackup = sc.CursorTop;
            });
        }
        public static void RestoreCursorPos()
        {
            Lock(() =>
            {
                sc.CursorLeft = _cursorLeftBackup;
                sc.CursorTop = _cursorTopBackup;
            });
        }
        public static void SetCursorLeft(int x) => Lock(() => sc.CursorLeft = FixX(x));
        public static void SetCursorTop(int y) => Lock(() => sc.CursorTop = FixY(y));
        public static int CursorLeft { 
            get { lock(ConsoleLock) { return sc.CursorLeft; } } 
        }
        public static int CursorTop
        {
            get { lock (ConsoleLock) { return sc.CursorTop; } }
        }
        public static Point CursorPos
        {
            get
            {
                lock (ConsoleLock)
                { return new Point(CursorLeft, CursorTop); }
            }
        }
        public static void SetCursorPos(Point p)
        {
            lock (ConsoleLock)
            {
                var x = p.X;
                var y = p.Y;
                FixCoords(ref x, ref y);
                sc.CursorLeft = x;
                sc.CursorTop = y;
            }
        }
        public static void SetCursorPos(int x,int y)
        {
            lock (ConsoleLock) {
                FixCoords(ref x, ref y);
                sc.CursorLeft = x;
                sc.CursorTop = y;
            }
        }
        public static void HideCur() => Lock(()=>sc.CursorVisible = false);
        public static void ShowCur() => Lock(()=>sc.CursorVisible = true);
        public static void Exit(int r=0) => Environment.Exit(r);

        public static void SetWorkArea(int wx,int wy,int width,int height)
        {
            lock (ConsoleLock)
            {
                var (x, y, w, h) = GetCoords(wx, wy, width, height);
                FixCoords(ref x, ref y);
                _workArea = new Rectangle(x, y, w, h);
                ApplyWorkArea();
            }
        }
        static void ApplyWorkArea()
        {
            lock (ConsoleLock)
            {
                if (_workArea.IsEmpty) return;
                try
                {
                    sc.WindowTop = 0;
                    sc.WindowLeft = 0;
                    sc.BufferWidth = sc.WindowWidth;
                    sc.BufferHeight = sc.WindowHeight;
                }
                catch (Exception) { }
            }
        }

        public static void Println(IEnumerable<string> ls) { foreach (var s in ls) Println(s); }
        public static void Print(IEnumerable<string> ls) { foreach (var s in ls) Print(s); }
        public static void Println(string s) => Print(s, true);
        public static void Print(string s) => Print(s, false);

        public static string Readln(string prompt = null)
        {
            lock (ConsoleLock)
            {
                if (prompt != null) Print(prompt);
            }
            return sc.ReadLine();
        }

        public static void EchoOn(string filepath)
        {
            if (!string.IsNullOrWhiteSpace(filepath) && _echoFileStream == null)
            {
                _echoFileStream = new FileStream(filepath, FileMode.Append, FileAccess.Write);
                _echoStreamWriter = new StreamWriter(_echoFileStream);
            }
        }
        public static void EchoOff()
        { 
            if (_echoFileStream!=null)
            {
                _outputStreamWriter.Flush();
                _outputStreamWriter.Close();
            }
        }
        
        public static object ExecCSharp(string csharpText)
        {
            try
            {
                var scriptKey = csharpText;
                if (!_csscripts.TryGetValue(scriptKey, out var script))
                {
                    script = CSharpScript.Create<object>(csharpText);
                    script.Compile();
                    _csscripts[scriptKey] = script;
                }
                var res = script.RunAsync();
                return res.Result.ReturnValue;
            }
            catch (CompilationErrorException ex)
            {
                LogError($"{csharpText}");
                LogError(string.Join(Environment.NewLine,ex.Diagnostics));
                return null;
            }
        }

        public static string GetBackwardHistory()
        {
            if (_historyIndex < 0)
                _historyIndex = _history.Count;
            if (_historyIndex>=1)
                _historyIndex--;
            System.Diagnostics.Debug.WriteLine($"{_historyIndex}");
            return (_historyIndex < 0 || _history.Count==0 || _historyIndex >= _history.Count) ? null : _history[_historyIndex];
        }

        public static string GetForwardHistory()
        {
            if (_historyIndex < 0 || _historyIndex >= _history.Count)
                _historyIndex = _history.Count;
            if (_historyIndex<_history.Count-1) _historyIndex++;

            System.Diagnostics.Debug.WriteLine($"{_historyIndex}");
            return (_historyIndex < 0 || _history.Count == 0 || _historyIndex >= _history.Count) ? null : _history[_historyIndex];
        }

        public static void HistoryAppend(string s)
        {
            _history.Add(s);
            _historyIndex = _history.Count - 1;
        }

        public static void ClearHistory() => _history.Clear();

        #endregion

        #region data to text operations

        static string DumpAsText(object o)
        {
            if (o == null)
                return DumpNullStringAsText ?? "";
            return o.ToString();
        }

        public static string Dump(object[] t)
        {
            return string.Join(',', t.Select(x => DumpAsText(x)));
        }

        #endregion

        #region UI elements methods

        static void RunUIElementWatcher()
        {
            if (_watcherThread != null)
                return;

            _watcherThread = new Thread(WatcherThreadImpl)
            {
                Name = "console tool watcher"
            };
            _watcherThread.Start();
        }

        static void WatcherThreadImpl()
        {
            int lastWinHeight,lastWinWidth,lastWinTop,lastWinLeft,w,h,l,t;
            lock (ConsoleLock)
            {
                lastWinHeight = sc.WindowHeight;
                lastWinWidth = sc.WindowWidth;
                lastWinTop = sc.WindowTop;
                lastWinLeft = sc.WindowLeft;
            }
            bool interrupted = false;
            try
            {
                while (true)
                {
                    lock (ConsoleLock)
                    {
                        h = sc.WindowHeight;
                        w = sc.WindowWidth;
                        l = sc.WindowLeft;
                        t = sc.WindowTop;
                        if (w != lastWinWidth || h != lastWinHeight || l != lastWinLeft || t != lastWinTop)
                        {
                            _redrawUIElementsEnabled = true;
                            RedrawUI(true);
                        }
                    }
                    lastWinHeight = h;
                    lastWinWidth = w;
                    lastWinLeft = l;
                    lastWinTop = t;
                    Thread.Sleep(500);
                }
            }
            catch (ThreadInterruptedException inex) { interrupted = true;  }
            catch (Exception ex)
            {
                LogError(ex);
            }
            if (!interrupted)
            {
                _watcherThread = null;
                RunUIElementWatcher();
            }
        }

        public static int AddFrame(
            Func<Frame, string> printContent,
            ConsoleColor backgroundColor,
            int x = 0,
            int y = -1,
            int w = -1,
            int h = 1,
            DrawStrategy drawStrategy = DrawStrategy.OnViewResizedOnly,
            bool mustRedrawBackground = true,
            int updateTimerInterval=0)
        {
            lock (ConsoleLock)
            {
                var o = new Frame(
                    printContent,
                    backgroundColor,
                    x,
                    y,
                    w,
                    h,
                    drawStrategy,
                    mustRedrawBackground,
                    updateTimerInterval);
                o.Draw();
                _uielements.Add(o.Id, o);
                RunUIElementWatcher();
                return o.Id;
            }
        }

        public static bool RemoveFrame(int id)
        {
            lock (ConsoleLock)
            {
                if (_uielements.ContainsKey(id))
                {
                    _uielements.Remove(id);
                    return true;
                }
                return false;
            }
        }

        static void RedrawUI(bool forceDraw = false,bool skipErase = false)
        {
            lock (ConsoleLock)
            {
                if (_redrawUIElementsEnabled && _uielements.Count > 0)
                {
                    _redrawUIElementsEnabled = false;
                    if (!skipErase && ClearOnViewResized && forceDraw)
                    {
                        Clear();
                    }
                    foreach (var o in _uielements)
                        o.Value.UpdateDraw(forceDraw & !ClearOnViewResized, forceDraw);

                    if (forceDraw) ApplyWorkArea();

                    _redrawUIElementsEnabled = true;
                }
            }
        }

        static void EraseUIElements()
        {
            lock (ConsoleLock)
            {
                if (_redrawUIElementsEnabled)
                    foreach (var o in _uielements)
                        o.Value.Erase();
            }
        }

        #endregion

        #region cli methods

        public static void RunSampleCLI(string prompt = null)
        {
            try
            {
                Clear();

                AddFrame((bar) =>
                {
                    var s = "".PadLeft(bar.ActualWidth, '-');
                    var t = "  dotnet console sdk - sample CLI";
                    var r = $"{Bdarkblue}{Cyan}{s}{Br}";
                    r += $"{Bdarkblue}{Cyan}|{t}{White}{"".PadLeft(Math.Max(0,bar.ActualWidth - 2 - t.Length))}{Cyan}|{Br}";
                    r += $"{Bdarkblue}{Cyan}{s}{Br}";
                    return r;
                }, ConsoleColor.DarkBlue, 0, 0, -1, 3, DrawStrategy.OnViewResizedOnly, false);

                SetCursorPos(0, 4);
                SetWorkArea(0, 4, -1, 10);
                Infos();
                LineBreak();

                AddFrame((bar) =>
                {
                    var r = $"{Bdarkblue}{Green}Cursor pos: {White}X={Cyan}{CursorLeft}{Green},{White}Y={Cyan}{CursorTop}{White}";
                    r += $" | {Green}bar pos: {White}X={Cyan}{bar.ActualX}{Green},{White}Y={Cyan}{bar.ActualY}{White}";
                    r += $" | {Cyan}{System.DateTime.Now}";
                    return r;
                }, ConsoleColor.DarkBlue,0,-1,-1,1,DrawStrategy.OnTime,true,500);

                Read(prompt);
#if NO
                var end = false;
                while (!end)
                {
                    try
                    {
                        /*var s =*/ 
                        //Println(s);
                    } catch (Exception interpretException)
                    {
                        LogError(interpretException);
                    }
                }
#endif
            } catch (Exception initException)
            {
                LogError(initException);
                Exit();
            }
        }

        public static void Read(string prompt = "")
        {            
            _readerThread = new Thread(() =>
            {
                try
                {
                    while (true)
                    {
                        var r = new StringBuilder();
                        Print(prompt);
                        var beginOfLineCurPos = CursorPos;
                        var eol = false;
                        while (!eol)
                        { 
                            var c = sc.ReadKey(true);
                            System.Diagnostics.Debug.WriteLine($"{c.KeyChar}={c.Key}");

                            // handle special caracters - edition mode, movement
                            var printed = false;
                            string printedStr="";
                            switch (c.Key)
                            {
                                case ConsoleKey.Enter:
                                    eol = true;
                                    break;
                                case ConsoleKey.Escape:
                                    lock (ConsoleLock)
                                    {
                                        HideCur();
                                        SetCursorPos(beginOfLineCurPos);                                        
                                        Print("".PadLeft(r.ToString().Length));
                                        SetCursorPos(beginOfLineCurPos);
                                        ShowCur();
                                        r.Clear();
                                    }
                                    break;
                                case ConsoleKey.Home:
                                    lock (ConsoleLock)
                                    {
                                        SetCursorLeft(beginOfLineCurPos.X);
                                    }
                                    break;
                                case ConsoleKey.End:
                                    lock (ConsoleLock)
                                    {
                                        SetCursorLeft(r.ToString().Length + beginOfLineCurPos.X);
                                    }
                                    break;
                                case ConsoleKey.Tab:
                                    lock (ConsoleLock)
                                    {
                                        printedStr = "".PadLeft(7,' ');
                                        printed = true;
                                    }
                                    break;
                                case ConsoleKey.LeftArrow:
                                    lock (ConsoleLock)
                                    {
                                        var x = CursorLeft;
                                        if (x > beginOfLineCurPos.X)
                                            SetCursorLeft(x - 1);
                                    }
                                    break;
                                case ConsoleKey.RightArrow:
                                    lock (ConsoleLock)
                                    {
                                        var x = CursorLeft;
                                        if (x < beginOfLineCurPos.X + r.ToString().Length)
                                            SetCursorLeft(x + 1);
                                    }
                                    break;
                                case ConsoleKey.Backspace:
                                    lock (ConsoleLock)
                                    {
                                        var x = CursorLeft;
                                        if (x > beginOfLineCurPos.X)
                                        {
                                            var x0 = x - beginOfLineCurPos.X -1;
                                            r.Remove(x0, 1);
                                            HideCur();
                                            SetCursorLeft(x - 1);
                                            var txt = r.ToString();
                                            if (x0<txt.Length)
                                                Print(txt.Substring(x0));
                                            Print(" ");
                                            SetCursorLeft(x - 1);
                                            ShowCur();
                                        }
                                    }
                                    break;
                                case ConsoleKey.Delete:
                                    lock (ConsoleLock)
                                    {
                                        var x = CursorLeft;
                                        var txt = r.ToString();
                                        var x0 = x - beginOfLineCurPos.X;
                                        if (x0 < txt.Length)
                                        {
                                            r.Remove(x0, 1);
                                            txt = r.ToString();
                                            HideCur();
                                            if (x0 < txt.Length)
                                                Print(txt.Substring(x0)+" ");
                                            else
                                                Print(" ");
                                            SetCursorLeft(x);
                                            ShowCur();
                                        }
                                    }
                                    break;
                                case ConsoleKey.UpArrow:
                                    var h = GetBackwardHistory();
                                    if (h!=null)
                                    {
                                        lock (ConsoleLock)
                                        {
                                            HideCur();
                                            SetCursorLeft(beginOfLineCurPos.X);
                                            Print("".PadLeft(r.ToString().Length,' '));
                                            SetCursorLeft(beginOfLineCurPos.X);
                                            r.Clear();
                                            r.Append(h);
                                            Print(h);
                                            ShowCur();
                                        }
                                    }
                                    break;
                                case ConsoleKey.DownArrow:
                                    var fh = GetForwardHistory();
                                    if (fh != null)
                                    {
                                        lock (ConsoleLock)
                                        {
                                            HideCur();
                                            SetCursorLeft(beginOfLineCurPos.X);
                                            Print("".PadLeft(r.ToString().Length, ' '));
                                            SetCursorLeft(beginOfLineCurPos.X);
                                            r.Clear();
                                            r.Append(fh);
                                            Print(fh);
                                            ShowCur();
                                        }
                                    }
                                    break;
                                default:
                                    printedStr = c.KeyChar+"";
                                    printed = true;
                                    break;
                            }

                            if (printed)
                            {
                                int x = 0; 
                                var insert = false;
                                lock (ConsoleLock)
                                {
                                    var x0 = CursorLeft;
                                    x = x0 - beginOfLineCurPos.X;
                                    var txt = r.ToString();
                                    insert = x - txt.Length < 0;
                                    if (insert)
                                    {
                                        var substr = txt.Substring(x);
                                        HideCur();
                                        SetCursorLeft(x0 + printedStr.Length);
                                        Print(substr);
                                        SetCursorLeft(x0);
                                        ShowCur();
                                    }
                                    ConsolePrint(printedStr, false);
                                }
                                if (!insert)
                                    r.Append(printedStr);
                                else
                                    r.Insert(x, printedStr);
                            }

                            if (eol) break;
                        }

                        // process input
                        var s = r.ToString();
                        lock (ConsoleLock)
                        {
                            LineBreak();
                            Println(s);
                        }
                        HistoryAppend(s);
                    }
                }
                catch { }
            })
            {
                Name = "input stream reader"
            };
            _readerThread.Start();
        }

        public static string Arg(int n)
        {
            if (_args == null) return null;
            if (_args.Length <= n) return null;
            return _args[n];
        }

        public static bool HasArgs => _args != null && _args.Length > 0;

        public static void SetArgs(string[] args)
        {
            _args = (string[])args?.Clone();
        }

        #region stream methods

        public static void OutputTo(string filepath = null)
        {
            if (filepath!=null)
            {
                _outputWriter = Console.Out;
                _outputFileStream = new FileStream(filepath, FileMode.Append, FileAccess.Write);
                _outputStreamWriter = new StreamWriter(_outputFileStream);
                sc.SetOut(_outputStreamWriter);
            } else
            {
                _outputStreamWriter.Flush();
                _outputStreamWriter.Close();
                sc.SetOut(_outputWriter);
            }
        }

        public static string TempPath => Path.Combine( Assembly.GetExecutingAssembly().Location , "Temp" );

        #endregion

        #endregion

        #region implementation methods

        public static string GetCmd(string cmd, string value = null)
        {
            if (value != null)
                return $"{CommandBlockBeginChar}{cmd}{CommandValueAssignationChar}{value}{CommandBlockEndChar}";
            return $"{CommandBlockBeginChar}{cmd}{CommandBlockEndChar}";
        }

        public static string GetCmd(KeyWords cmd, string value = null)
        {
            if (value != null)
                return $"{CommandBlockBeginChar}{cmd}{CommandValueAssignationChar}{value}{CommandBlockEndChar}";
            return $"{CommandBlockBeginChar}{cmd}{CommandBlockEndChar}";
        }

        static void TraceError(string s) => LogError(s);

        static ConsoleColor GetColor(string colorName)
        {
            return (ConsoleColor)Enum.Parse(typeof(ConsoleColor), colorName);
        }
        static ConsoleColor ParseColor(object c) {

            if (Enum.TryParse<ConsoleColor>((string)c, true, out ConsoleColor r))
                return r;
            if (TraceCommandErrors) TraceError($"invalid color name: {c}");
            return DefaultForeground;
        }

        static int GetCursorX(object x)
        {
            if (x != null && x is string s && !string.IsNullOrWhiteSpace(s)
                && int.TryParse(s, out var v))
                return v;
            if (TraceCommandErrors) TraceError($"wrong cursor x: {x}");
            lock (ConsoleLock)
            {
                return sc.CursorLeft;
            }
        }
        static int GetCursorY(object x)
        {
            if (x != null && x is string s && !string.IsNullOrWhiteSpace(s)
                && int.TryParse(s, out var v))
                return v;
            if (TraceCommandErrors) TraceError($"wrong cursor y: {x}");
            lock (ConsoleLock)
            {
                return sc.CursorTop;
            }
        }

        static void ConsolePrint(string s, bool lineBreak = false)
        {
            lock (ConsoleLock)
            {
                sc.Write(s);
                if (lineBreak) sc.WriteLine(string.Empty);
            }
        }

        static void Print(object s, bool lineBreak = false, bool preserveColors = false, bool parseCommands = true)
        {
            lock (ConsoleLock)
            {
                var redrawUIElementsEnabled = _redrawUIElementsEnabled;
                _redrawUIElementsEnabled = false;
                if (!preserveColors && SaveColors)
                {
                    BackupBackground();
                    BackupForeground();
                }

                if (!preserveColors && EnableColors)
                {
                    sc.ForegroundColor = DefaultForeground;
                    sc.BackgroundColor = DefaultBackground;
                }

                if (s == null)
                {
                    if (DumpNullStringAsText != null)
                        ConsolePrint(DumpNullStringAsText, false);
                }
                else
                {
                    if (parseCommands)
                        ParseTextAndApplyCommands(s.ToString(), false);
                    else
                        ConsolePrint(s.ToString(), false);
                }

                if (!preserveColors && SaveColors)
                {
                    RestoreBackground();
                    RestoreForeground();
                }

                if (lineBreak) LineBreak();

                _redrawUIElementsEnabled = redrawUIElementsEnabled;
                RedrawUI(redrawUIElementsEnabled, true);
            }
        }

        static void ParseTextAndApplyCommands(string s, bool lineBreak = false, string tmps = "")
        {
            lock (ConsoleLock)
            {
                int i = 0;
                KeyValuePair<string, CommandDelegate>? cmd = null;
                int n = s.Length;
                bool isAssignation = false;
                while (cmd == null && i < n)
                {
                    foreach (var ccmd in _drtvs)
                    {
                        if (s.IndexOf(CommandBlockBeginChar + ccmd.Key, i) == i)
                        {
                            cmd = ccmd;
                            isAssignation = ccmd.Key.EndsWith("=");
                            break;
                        }
                    }
                    if (cmd == null)
                        tmps += s.Substring(i, 1);
                    i++;
                }
                if (cmd == null)
                {
                    ConsolePrint(tmps, false);
                    return;
                }

                if (!string.IsNullOrEmpty(tmps))
                    ConsolePrint(tmps);

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
                                value = s.Substring(firstCommandEndIndex, k - firstCommandEndIndex);
                                k += CodeBlockEnd.Length;
                            }
                            else
                            {
                                ConsolePrint(s);
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
                    result = cmd.Value.Value(value);
                }
                else
                {
                    result = cmd.Value.Value(null);
                }
                if (result != null)
                    Print(result, false, true);

                if (firstCommandSeparatorCharIndex > -1)
                    s = CommandBlockBeginChar + s.Substring(firstCommandSeparatorCharIndex + 1 /*+ i*/ );
                else
                {
                    if (j + 1 < s.Length)
                        s = s.Substring(j + 1);
                    else
                        s = string.Empty;
                }

                if (!string.IsNullOrEmpty(s))
                    ParseTextAndApplyCommands(s, lineBreak);
            }
        }

        #endregion

        #region commands shortcuts

        public static string Bblack => GetCmd(KeyWords.b+"", "black");
        public static string Bdarkblue => GetCmd(KeyWords.b , "darkblue");
        public static string Bdarkgreen => GetCmd(KeyWords.b , "darkgreen");
        public static string Bdarkcyan => GetCmd(KeyWords.b , "darkcyan");
        public static string Bdarkred => GetCmd(KeyWords.b , "darkred");
        public static string Bdarkmagenta => GetCmd(KeyWords.b , "darkmagenta");
        public static string Bdarkyellow => GetCmd(KeyWords.b , "darkyelllow");
        public static string Bgray => GetCmd(KeyWords.b , "gray");
        public static string Bdarkgray => GetCmd(KeyWords.b , "darkgray");
        public static string Bblue => GetCmd(KeyWords.b , "blue");
        public static string Bgreen => GetCmd(KeyWords.b , "green");
        public static string Bcyan => GetCmd(KeyWords.b , "cyan");
        public static string Bred => GetCmd(KeyWords.b , "red");
        public static string Bmagenta => GetCmd(KeyWords.b , "magenta");
        public static string Byellow => GetCmd(KeyWords.b , "yellow");
        public static string Bwhite => GetCmd(KeyWords.b , "white");
        public static string Black => GetCmd(KeyWords.f , "black");
        public static string Darkblue => GetCmd(KeyWords.f , "darkblue");
        public static string Darkgreen => GetCmd(KeyWords.f , "darkgreen");
        public static string Darkcyan => GetCmd(KeyWords.f , "darkcyan");
        public static string Darkred => GetCmd(KeyWords.f , "darkred");
        public static string Darkmagenta => GetCmd(KeyWords.f , "darkmagenta");
        public static string Darkyellow => GetCmd(KeyWords.f , "darkyelllow");
        public static string Gray => GetCmd(KeyWords.f , "gray");
        public static string Darkgray => GetCmd(KeyWords.f , "darkgray");
        public static string Blue => GetCmd(KeyWords.f , "blue");
        public static string Green => GetCmd(KeyWords.f , "green");
        public static string Cyan => GetCmd(KeyWords.f , "cyan");
        public static string Red => GetCmd(KeyWords.f , "red");
        public static string Magenta => GetCmd(KeyWords.f , "magenta");
        public static string Yellow => GetCmd(KeyWords.f , "yellow");
        public static string White => GetCmd(KeyWords.f , "white");

        public static string Bkf => GetCmd(KeyWords.bkf );
        public static string Rf => GetCmd(KeyWords.rsf );
        public static string Bkb => GetCmd(KeyWords.bkb );
        public static string Rb => GetCmd(KeyWords.rsb );
        public static string Cl => GetCmd(KeyWords.cl );
        public static string Br => GetCmd(KeyWords.br );

        public static string B(ConsoleColor c) => GetCmd(KeyWords.b , c+"");
        public static string F(ConsoleColor c) => GetCmd(KeyWords.f , c+"");

        public static string Bkcr => GetCmd(KeyWords.bkcr );
        public static string Rscr => GetCmd(KeyWords.rscr );
        public static string Crx(int x) => GetCmd(KeyWords.crx , x +"");
        public static string Cry(int y) => GetCmd(KeyWords.cry , y +"");
        public static string Cr(int x, int y) => $"{GetCmd(KeyWords.crx , x +"" )}{GetCmd(KeyWords.cry , y+"" )}";

        public static string Exec(string csharpText) => GetCmd(KeyWords.exec , csharpText);

        #endregion

    }
}
