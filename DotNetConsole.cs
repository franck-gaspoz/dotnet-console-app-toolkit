#define dbg

using DotNetConsoleAppToolkit.Component.UI;
using DotNetConsoleAppToolkit.Console;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using static DotNetConsoleAppToolkit.Component.UI.UIElement;
using static DotNetConsoleAppToolkit.Lib.Str;
using sc = System.Console;

[assembly: AssemblyDescription("Dot Net Console App Toolkit kernel commands module")]
[assembly: AssemblyCopyright("© June 2020")]
[assembly: AssemblyTrademark("franck.gaspoz@gmail.com")]
namespace DotNetConsoleAppToolkit
{
    /// <summary>
    /// dotnet core sdk helps build fastly nice console applications
    /// <para>
    /// slowness due to:
    /// - many system calls on both linux (ConsolePal.Unix.cs) and windows 
    /// - use of interop on each console method in windows (ConsolePal.Windows.cs)
    /// workarounds:
    /// - 'retains mode' : echo in a buffer in order to output as much text as possible in one time
    /// - needs a refactorization: 
    /// -- dotnetconsole api placed on top of streams out,error as a wrapper)
    /// -- explode DotNetConsole in small parts
    /// </para>
    /// </summary>
    public static class DotNetConsole
    {
        #region attributes

        #region streams : entry points to DotNetConsole output operations

        public static readonly ConsoleTextWriterWrapper Out = new ConsoleTextWriterWrapper(sc.Out);
        public static readonly TextWriterWrapper Err = new ConsoleTextWriterWrapper(sc.Error);

        #endregion

        #region work area settings

        static WorkArea _workArea = new WorkArea();
        public static WorkArea WorkArea => new WorkArea(_workArea);
        public static bool InWorkArea => !_workArea.Rect.IsEmpty;
        public static EventHandler ViewSizeChanged;
        public static EventHandler<WorkAreaScrollEventArgs> WorkAreaScrolled;
        public static bool EnableConstraintConsolePrintInsideWorkArea = false;

        #endregion

        public static bool IsErrorRedirected = false;
        public static bool IsOutputRedirected = false;

        public static int UIWatcherThreadDelay = 500;
        public static ViewResizeStrategy ViewResizeStrategy = ViewResizeStrategy.FitViewSize;
        public static bool ClearOnViewResized = true;      // false not works properly in Windows Terminal + fit view size
        
        public static bool SaveColors = /*true*/ false; /*bug fix*/ // TODO: remove
        
        public static bool TraceCommandErrors = true;
        public static bool DumpExceptions = true;
        public static ConsoleColor DefaultForeground = ConsoleColor.White;
        public static ConsoleColor DefaultBackground = ConsoleColor.Black;

        public static char CommandBlockBeginChar = '(';
        public static char CommandBlockEndChar = ')';
        public static char CommandSeparatorChar = ',';
        public static char CommandValueAssignationChar = '=';
        public static string CodeBlockBegin = "[[";
        public static string CodeBlockEnd = "]]";

        public static bool ForwardLogsToSystemDiagnostics = true;
        public static int TabLength = 7;

        static Thread _watcherThread;
        static readonly Dictionary<int, UIElement> _uielements = new Dictionary<int, UIElement>();

        static TextWriter _errorWriter;
        static StreamWriter _errorStreamWriter;
        static FileStream _errorFileStream;
        static TextWriter _outputWriter;
        static StreamWriter _outputStreamWriter;
        static FileStream _outputFileStream;

        static readonly Dictionary<string, Script<object>> _csscripts = new Dictionary<string, Script<object>>();

        static string[] _crlf = { Environment.NewLine };

        #endregion

        #region log methods

        public static void LogError(Exception ex)
        {
            if (ForwardLogsToSystemDiagnostics) System.Diagnostics.Debug.WriteLine(ex + "");
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
                    .Select(x => ColorSettings.Error + x);
                Errorln(ls);
            }
        }

        public static void LogException(Exception ex, string message = "")
        {
            if (ForwardLogsToSystemDiagnostics) System.Diagnostics.Debug.WriteLine(message + _crlf + ex + "");
            var ls = new List<string>();
            if (DumpExceptions)
            {
                ls = (ex + "").Split(_crlf, StringSplitOptions.None)
                .Select(x => ColorSettings.Error + x)
                .ToList();
                if (message != null) ls.Insert(0, $"{ColorSettings.Error}{message}");
            } else
                ls.Insert(0, $"{ColorSettings.Error}{message}: {ex.Message}");
            Errorln(ls);
        }

        public static void LogError(string s)
        {
            if (ForwardLogsToSystemDiagnostics) System.Diagnostics.Debug.WriteLine(s);
            var ls = (s + "").Split(_crlf, StringSplitOptions.None)
                .Select(x => ColorSettings.Error + x);
            Errorln(ls);
        }

        public static void LogWarning(string s)
        {
            if (ForwardLogsToSystemDiagnostics) System.Diagnostics.Debug.WriteLine(s);
            var ls = (s + "").Split(_crlf, StringSplitOptions.None)
                .Select(x => ColorSettings.Warning + x);
            Errorln(ls);
        }

        public static void Log(string s)
        {
            if (ForwardLogsToSystemDiagnostics) System.Diagnostics.Debug.WriteLine(s);
            var ls = (s + "").Split(_crlf, StringSplitOptions.None)
                .Select(x => ColorSettings.Log + x);
            Println(ls);
        }

        #endregion

        public static void Println(IEnumerable<string> ls, bool ignorePrintDirectives = false) { foreach (var s in ls) Println(s, ignorePrintDirectives); }
        public static void Print(IEnumerable<string> ls, bool lineBreak = false, bool ignorePrintDirectives = false) { foreach (var s in ls) Print(s, lineBreak, ignorePrintDirectives); }
        public static void Println(string s = "", bool ignorePrintDirectives = false) => Out.Print(s, true, false, !ignorePrintDirectives);
        public static void Print(string s = "", bool lineBreak = false, bool ignorePrintDirectives = false) => Out.Print(s, lineBreak, false, !ignorePrintDirectives);
        public static void Println(char s, bool ignorePrintDirectives = false) => Out.Print(s + "", true, false, !ignorePrintDirectives);
        public static void Print(char s, bool lineBreak = false, bool ignorePrintDirectives = false) => Print(s + "", lineBreak, !ignorePrintDirectives);

        public static void Error(string s = "") => Error(s, false);
        public static void Errorln(string s = "") => Error(s, true);
        public static void Errorln(IEnumerable<string> ls) { foreach (var s in ls) Errorln(s); }
        public static void Error(IEnumerable<string> ls) { foreach (var s in ls) Error(s); }
        public static void Error(string s, bool lineBreak = false)
        {
            lock (Out.Lock)
            {
                Out.RedirecToErr = true;
                Print($"{ColorSettings.Error}{s}{ColorSettings.Default}", lineBreak);
                Out.RedirecToErr = false;
            }
        }

        public static void Warning(string s = "") => Warning(s, false);
        public static void Warningln(string s = "") => Warning(s, true);
        public static void Warningln(IEnumerable<string> ls) { foreach (var s in ls) Errorln(s); }
        public static void Warning(IEnumerable<string> ls) { foreach (var s in ls) Error(s); }
        public static void Warning(string s, bool lineBreak = false)
        {
            lock (Out.Lock)
            {
                Print($"{ColorSettings.Warning}{s}{ColorSettings.Default}", lineBreak);
            }
        }

        public static string Readln(string prompt = null)
        {
            lock (Out.Lock)
            {
                if (prompt != null) Print(prompt);
            }
            return sc.ReadLine();
        }

        public static void Infos()
        {
            Out.Locked(() =>
            {
                Println($"OS={Environment.OSVersion} {(Environment.Is64BitOperatingSystem ? "64" : "32")}bits");
                Println($"{White}{Bkf}{ColorSettings.HighlightIdentifier}window:{Rf} left={ColorSettings.Numeric}{sc.WindowLeft}{Rf},top={ColorSettings.Numeric}{sc.WindowTop}{Rf},width={ColorSettings.Numeric}{sc.WindowWidth}{Rf},height={ColorSettings.Numeric}{sc.WindowHeight}{Rf},largest width={ColorSettings.Numeric}{sc.LargestWindowWidth}{Rf},largest height={ColorSettings.Numeric}{sc.LargestWindowHeight}{Rf}");
                Println($"{ColorSettings.HighlightIdentifier}buffer:{Rf} width={ColorSettings.Numeric}{sc.BufferWidth}{Rf},height={ColorSettings.Numeric}{sc.BufferHeight}{Rf} | input encoding={ColorSettings.Numeric}{sc.InputEncoding.EncodingName}{Rf} | output encoding={ColorSettings.Numeric}{sc.OutputEncoding.EncodingName}{Rf}");
                Println($"number lock={ColorSettings.Numeric}{sc.NumberLock}{Rf} | capslock={ColorSettings.Numeric}{sc.CapsLock}{Rf}");            // TODO: not supported on linux ubuntu 18.04 wsl
                Println($"cursor visible={ColorSettings.Numeric}{sc.CursorVisible}{Rf} | cursor size={ColorSettings.Numeric}{sc.CursorSize}");     // TODO: not supported on linux ubuntu 18.04 wsl
            });
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
                LogError(string.Join(Environment.NewLine, ex.Diagnostics));
                return null;
            }
        }

        public static void Exit(int r = 0) => Environment.Exit(r);

        #region work area operations

        public static void SetWorkArea(string id, int wx, int wy, int width, int height)
        {
            lock (Out.Lock)
            {
                _workArea = new WorkArea(id, wx, wy, width, height);
                ApplyWorkArea();
                EnableConstraintConsolePrintInsideWorkArea = true;
            }
        }

        public static void UnsetWorkArea()
        {
            _workArea = new WorkArea();
            EnableConstraintConsolePrintInsideWorkArea = false;
        }

        public static ActualWorkArea ActualWorkArea(bool fitToVisibleArea = true)
        {
            var x0 = _workArea.Rect.IsEmpty ? 0 : _workArea.Rect.X;
            var y0 = _workArea.Rect.IsEmpty ? 0 : _workArea.Rect.Y;
            var w0 = _workArea.Rect.IsEmpty ? -1 : _workArea.Rect.Width;
            var h0 = _workArea.Rect.IsEmpty ? -1 : _workArea.Rect.Height;
            var (x, y, w, h) = GetCoords(x0, y0, w0, h0, fitToVisibleArea);
            return new ActualWorkArea(_workArea.Id, x, y, w, h);
        }

        public static void ApplyWorkArea(bool viewSizeChanged = false)
        {
            if (_workArea.Rect.IsEmpty) return;
            lock (Out.Lock)
            {
                if (ViewResizeStrategy != ViewResizeStrategy.HostTerminalDefault &&
                    (!viewSizeChanged ||
                    (viewSizeChanged && ViewResizeStrategy == ViewResizeStrategy.FitViewSize)))
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

        public static void SetCursorAtWorkAreaTop()
        {
            if (_workArea.Rect.IsEmpty) return;     // TODO: set cursor even if workarea empty?
            lock (Out.Lock)
            {
                Out.SetCursorPos(_workArea.Rect.X, _workArea.Rect.Y);
            }
        }

        #endregion

        #region UI operations

        static void RunUIElementWatcher()
        {
            if (_watcherThread != null)
                return;

            _watcherThread = new Thread(WatcherThreadImpl)
            {
                Name = "ui watcher"
            };
            _watcherThread.Start();
        }

        static void WatcherThreadImpl()
        {
            int lastWinHeight,lastWinWidth,lastWinTop,lastWinLeft,w,h,l,t;
            lock (Out.Lock)
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
                    lock (Out.Lock)
                    {
                        h = sc.WindowHeight;
                        w = sc.WindowWidth;
                        l = sc.WindowLeft;
                        t = sc.WindowTop;
                        if (w != lastWinWidth || h != lastWinHeight || l != lastWinLeft || t != lastWinTop)
                        {
                            if (ViewResizeStrategy != ViewResizeStrategy.HostTerminalDefault)
                            {
                                RedrawUIElementsEnabled = true;
                                UpdateUI(true);                               
                            }
                        }
                    }
                    lastWinHeight = h;
                    lastWinWidth = w;
                    lastWinLeft = l;
                    lastWinTop = t;
                    Thread.Sleep(UIWatcherThreadDelay);
                }
            }
            catch (ThreadInterruptedException) { interrupted = true;  }
            catch (Exception ex)
            {
                LogError("ui watcher crashed: " + ex.Message);
            }
            if (!interrupted)
            {
                _watcherThread = null;
                RunUIElementWatcher();
            }
        }

        public static int AddFrame(
            GetContentDelegate getContent,
            ConsoleColor backgroundColor,
            int x = 0,
            int y = -1,
            int w = -1,
            int h = 1,
            DrawStrategy drawStrategy = DrawStrategy.OnViewResizedOnly,
            bool mustRedrawBackground = true,
            int updateTimerInterval=0)
        {
            lock (Out.Lock)
            {
                var o = new Frame(
                    getContent,
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
            lock (Out.Lock)
            {
                if (_uielements.ContainsKey(id))
                {
                    _uielements.Remove(id);
                    return true;
                }
                return false;
            }
        }

        public static void UpdateUI(bool viewSizeChanged=false,bool enableViewSizeChangedEvent=true)
        {
            lock (Out.Lock)
            {
                if (RedrawUIElementsEnabled)
                {
                    RedrawUIElementsEnabled = false;
                    var cursorPosBackup = Out.CursorPos;

                    if (ViewResizeStrategy == ViewResizeStrategy.FitViewSize
                        && viewSizeChanged)
                    {
                        if (ClearOnViewResized)
                            Out.ClearScreen();
                    }

                    foreach (var o in _uielements)
                        o.Value.UpdateDraw(viewSizeChanged);

                    if (viewSizeChanged)
                    {
                        ApplyWorkArea(viewSizeChanged);
                        if (ViewResizeStrategy == ViewResizeStrategy.FitViewSize
                            && ClearOnViewResized)
                            if (_workArea.Rect.IsEmpty)
                                Out.SetCursorPos(cursorPosBackup);
                            else
                                SetCursorAtWorkAreaTop();
                        if (enableViewSizeChangedEvent)
                            ViewSizeChanged?.Invoke(null,EventArgs.Empty);
                    }

                    RedrawUIElementsEnabled = true;
                }
            }
        }

        static void EraseUIElements()
        {
            lock (Out.Lock)
            {
                if (RedrawUIElementsEnabled)
                    foreach (var o in _uielements)
                        o.Value.Erase();
            }
        }

        #endregion
        
        #region stream methods

        public static void RedirectOut(StreamWriter sw)
        {
            if (sw != null)
            {
                Out.Redirect(sw);
                _outputWriter = sc.Out;
                sc.SetOut(sw);
                IsOutputRedirected = true;
            } else
            {
                Out.Redirect((TextWriter)null);
                sc.SetOut(_outputWriter);
                _outputWriter = null;
                IsOutputRedirected = false;
            }
        }

        public static void RedirectErr(TextWriter sw)
        {
            if (sw != null)
            {
                Err.Redirect(sw);
                _errorWriter = sc.Error;
                sc.SetError(sw);
                IsErrorRedirected = true;
            }
            else
            {
                Err.Redirect((TextWriter)null);
                sc.SetError(_errorWriter);
                _errorWriter = null;
                IsErrorRedirected = false;
            }
        }

        public static void RedirectOut(string filepath = null)
        {
            if (filepath!=null)
            {
                _outputWriter = sc.Out;
                _outputFileStream = new FileStream(filepath, FileMode.Append, FileAccess.Write);
                _outputStreamWriter = new StreamWriter(_outputFileStream);
                sc.SetOut(_outputStreamWriter);
                Out.Redirect(_outputStreamWriter);
            } else
            {
                _outputStreamWriter.Flush();
                _outputStreamWriter.Close();
                _outputStreamWriter = null;
                sc.SetOut(_outputWriter);
                _outputWriter = null;
                Out.Redirect((string)null);
            }
        }

        public static void RedirectErr(string filepath = null)
        {
            if (filepath != null)
            {
                _errorWriter = sc.Error;
                _errorFileStream = new FileStream(filepath, FileMode.Append, FileAccess.Write);
                _errorStreamWriter = new StreamWriter(_errorFileStream);
                sc.SetOut(_errorStreamWriter);
                Err.Redirect(_errorStreamWriter);
            }
            else
            {
                _errorStreamWriter.Flush();
                _errorStreamWriter.Close();
                _errorStreamWriter = null;
                sc.SetOut(_errorWriter);
                _errorWriter = null;
                Err.Redirect((string)null);
            }
        }

        #endregion

        #region folders

        public static string TempPath => Path.Combine( Environment.CurrentDirectory , "Temp" );

        #endregion

        #region implementation methods

        public static string GetCmd(string cmd, string value = null)
        {
            if (value != null)
                return $"{CommandBlockBeginChar}{cmd}{CommandValueAssignationChar}{value}{CommandBlockEndChar}";
            return $"{CommandBlockBeginChar}{cmd}{CommandBlockEndChar}";
        }

        public static string GetCmd(PrintDirectives cmd, string value = null)
        {
            if (value != null)
                return $"{CommandBlockBeginChar}{cmd}{CommandValueAssignationChar}{value}{CommandBlockEndChar}";
            return $"{CommandBlockBeginChar}{cmd}{CommandBlockEndChar}";
        }

        static int GetCursorX(object x)
        {
            if (x != null && x is string s && !string.IsNullOrWhiteSpace(s)
                && int.TryParse(s, out var v))
                return v;
            if (TraceCommandErrors) LogError($"wrong cursor x: {x}");
            lock (Out.Lock)
            {
                return sc.CursorLeft;
            }
        }

        static int GetCursorY(object x)
        {
            if (x != null && x is string s && !string.IsNullOrWhiteSpace(s)
                && int.TryParse(s, out var v))
                return v;
            if (TraceCommandErrors) LogError($"wrong cursor y: {x}");
            lock (Out.Lock)
            {
                return sc.CursorTop;
            }
        }

        #endregion

        #region commands shortcuts

        public static string Clleft => GetCmd(PrintDirectives.clleft);
        public static string Clright => GetCmd(PrintDirectives.clright);
        public static string Fillright => GetCmd(PrintDirectives.fillright);
        public static string Cl => GetCmd(PrintDirectives.cl);
        public static string Chome => GetCmd(PrintDirectives.chome);

        public static string Lion => GetCmd(PrintDirectives.lion);
        public static string Bon => GetCmd(PrintDirectives.bon);
        public static string Blon => GetCmd(PrintDirectives.blon);

        public static string Cleft => GetCmd(PrintDirectives.cleft);
        public static string Cright => GetCmd(PrintDirectives.cright);
        public static string Cup => GetCmd(PrintDirectives.cup);
        public static string Cdown => GetCmd(PrintDirectives.cdown);
        public static string Cnleft(int n) => GetCmd(PrintDirectives.cleft+"",n+"");
        public static string Cnright(int n) => GetCmd(PrintDirectives.cright + "", n + "");
        public static string Cnup(int n) => GetCmd(PrintDirectives.cup + "", n + "");
        public static string Cndown(int n) => GetCmd(PrintDirectives.cdown + "", n + "");

        public static string Invon => GetCmd(PrintDirectives.invon);
        public static string Uon => GetCmd(PrintDirectives.uon);
        public static string Tdoff => GetCmd(PrintDirectives.tdoff);
        
        public static string DefaultBackgroundCmd => GetCmd(PrintDirectives.b + "", DefaultBackground.ToString().ToLower());
        public static string DefaultForegroundCmd => GetCmd(PrintDirectives.f + "", DefaultForeground.ToString().ToLower());
        public static string Rdc => GetCmd(PrintDirectives.rdc);

        public static string Bblack => GetCmd(PrintDirectives.b+"", "black");
        public static string Bdarkblue => GetCmd(PrintDirectives.b , "darkblue");
        public static string Bdarkgreen => GetCmd(PrintDirectives.b , "darkgreen");
        public static string Bdarkcyan => GetCmd(PrintDirectives.b , "darkcyan");
        public static string Bdarkred => GetCmd(PrintDirectives.b , "darkred");
        public static string Bdarkmagenta => GetCmd(PrintDirectives.b , "darkmagenta");
        public static string Bdarkyellow => GetCmd(PrintDirectives.b , "darkyellow");
        public static string Bgray => GetCmd(PrintDirectives.b , "gray");
        public static string Bdarkgray => GetCmd(PrintDirectives.b , "darkgray");
        public static string Bblue => GetCmd(PrintDirectives.b , "blue");
        public static string Bgreen => GetCmd(PrintDirectives.b , "green");
        public static string Bcyan => GetCmd(PrintDirectives.b , "cyan");
        public static string Bred => GetCmd(PrintDirectives.b , "red");
        public static string Bmagenta => GetCmd(PrintDirectives.b , "magenta");
        public static string Byellow => GetCmd(PrintDirectives.b , "yellow");
        public static string Bwhite => GetCmd(PrintDirectives.b , "white");
        public static string Black => GetCmd(PrintDirectives.f , "black");
        public static string Darkblue => GetCmd(PrintDirectives.f , "darkblue");
        public static string Darkgreen => GetCmd(PrintDirectives.f , "darkgreen");
        public static string Darkcyan => GetCmd(PrintDirectives.f , "darkcyan");
        public static string Darkred => GetCmd(PrintDirectives.f , "darkred");
        public static string Darkmagenta => GetCmd(PrintDirectives.f , "darkmagenta");
        public static string Darkyellow => GetCmd(PrintDirectives.f , "darkyellow");
        public static string Gray => GetCmd(PrintDirectives.f , "gray");
        public static string Darkgray => GetCmd(PrintDirectives.f , "darkgray");
        public static string Blue => GetCmd(PrintDirectives.f , "blue");
        public static string Green => GetCmd(PrintDirectives.f , "green");
        public static string Cyan => GetCmd(PrintDirectives.f , "cyan");
        public static string Red => GetCmd(PrintDirectives.f , "red");
        public static string Magenta => GetCmd(PrintDirectives.f , "magenta");
        public static string Yellow => GetCmd(PrintDirectives.f , "yellow");
        public static string White => GetCmd(PrintDirectives.f , "white");

        public static string Bkf => GetCmd(PrintDirectives.bkf );
        public static string Rf => GetCmd(PrintDirectives.rsf );
        public static string Bkb => GetCmd(PrintDirectives.bkb );
        public static string Rb => GetCmd(PrintDirectives.rsb );
        public static string Cls => GetCmd(PrintDirectives.cls );
        public static string Br => GetCmd(PrintDirectives.br );

        public static string B(ConsoleColor c) => GetCmd(PrintDirectives.b , c+"");
        public static string B8(ConsoleColor c) => GetCmd(PrintDirectives.b8 , c+"");
        public static string B24(ConsoleColor c) => GetCmd(PrintDirectives.b24 , c+"");

        public static string F(ConsoleColor c) => GetCmd(PrintDirectives.f , c+"");
        public static string F8(ConsoleColor c) => GetCmd(PrintDirectives.f8 , c+"");
        public static string F24(ConsoleColor c) => GetCmd(PrintDirectives.f24 , c+"");

        public static string Bkcr => GetCmd(PrintDirectives.bkcr );
        public static string Rscr => GetCmd(PrintDirectives.rscr );
        public static string Crx(int x) => GetCmd(PrintDirectives.crx , x +"");
        public static string Cry(int y) => GetCmd(PrintDirectives.cry , y +"");
        public static string Cr(int x, int y) => $"{GetCmd(PrintDirectives.crx , x +"" )}{GetCmd(PrintDirectives.cry , y+"" )}";

        public static string Exec(string csharpText) => GetCmd(PrintDirectives.exec , csharpText);

        public static string Tab => "".PadLeft(TabLength, ' ');

        #endregion

    }
}
