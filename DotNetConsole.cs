#define dbg

using DotNetConsoleSdk.Component.CommandLine.CommandLineReader;
using DotNetConsoleSdk.Component.UI;
using DotNetConsoleSdk.Console;
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
using static DotNetConsoleSdk.Component.UI.UIElement;
using static DotNetConsoleSdk.Lib.Str;
using sc = System.Console;

[assembly: AssemblyDescription("DotNetConsoleToolkit core module")]
[assembly: AssemblyCopyright("© Franck Gaspoz 2020")]
[assembly: AssemblyTrademark("franck.gaspoz@gmail.com")]
namespace DotNetConsoleSdk
{
    /// <summary>
    /// dotnet core sdk helps build fastly nice console applications
    /// </summary>
    public static class DotNetConsole
    {
        #region attributes

        public static bool RedirectOutToError = false;
        public static bool FileEchoDumpDebugInfo = true;
        public static bool FileEchoCommands = true;
        public static bool FileEchoAutoFlush = true;
        public static bool FileEchoAutoLineBreak = true;
        public static bool EnableConstraintConsolePrintInsideWorkArea = true;
        public static int CropX = -1;
        public static int UIWatcherThreadDelay = 500;
        public static ViewResizeStrategy ViewResizeStrategy = ViewResizeStrategy.FitViewSize;
        public static bool ClearOnViewResized = true;      // false not works properly in Windows Terminal + fit view size
        public static bool SaveColors = true;
        public static bool TraceCommandErrors = true;
        public static bool EnableColors = true;
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
        static (string id,Rectangle rect) _workArea = (null,Rectangle.Empty);
        public static (string id,Rectangle rect) WorkArea => (_workArea.id,new Rectangle(_workArea.rect.X, _workArea.rect.Y, _workArea.rect.Width, _workArea.rect.Height));
        public static int TabLength = 7;

        public static EventHandler ViewSizeChanged;
        public static EventHandler<WorkAreaScrollEventArgs> WorkAreaScrolled;
        
        static int _cursorLeftBackup;
        static int _cursorTopBackup;
        static ConsoleColor _backgroundBackup = ConsoleColor.Black;
        static ConsoleColor _foregroundBackup = ConsoleColor.White;
        static readonly string[] _crlf = { Environment.NewLine };

        static Thread _watcherThread;
        static readonly Dictionary<int, UIElement> _uielements = new Dictionary<int, UIElement>();
        
        static TextWriter _outputWriter;
        static StreamWriter _outputStreamWriter;
        static FileStream _outputFileStream;
        static StreamWriter _echoStreamWriter;
        static FileStream _echoFileStream;

        static readonly Dictionary<string, Script<object>> _csscripts = new Dictionary<string, Script<object>>();

        public static object ConsoleLock = new object();

        public static bool FileEchoEnabled => _echoStreamWriter != null;

        #endregion

        #region log methods

        public static void LogError(Exception ex)
        {
            if (ForwardLogsToSystemDiagnostics) System.Diagnostics.Debug.WriteLine(ex+"");
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
                    .Select(x => ((EnableColors) ? $"{ColorSettings.Error}" : "") + x);
                Errorln(ls);
            }
        }

        public static void LogException(Exception ex,string message = "")
        {
            if (ForwardLogsToSystemDiagnostics) System.Diagnostics.Debug.WriteLine(message+_crlf+ex+"");
            var ls = new List<string>();
            if (DumpExceptions)
            {
                ls = (ex + "").Split(_crlf, StringSplitOptions.None)
                .Select(x => ((EnableColors) ? $"{ColorSettings.Error}" : "") + x)
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
                .Select(x => ((EnableColors) ? $"{ColorSettings.Error}" : "") + x);
            Errorln(ls);
        }

        public static void LogWarning(string s)
        {
            if (ForwardLogsToSystemDiagnostics) System.Diagnostics.Debug.WriteLine(s);
            var ls = (s + "").Split(_crlf, StringSplitOptions.None)
                .Select(x => ((EnableColors) ? $"{ColorSettings.Warning}" : "") + x);
            Errorln(ls);
        }

        public static void Log(string s)
        {
            if (ForwardLogsToSystemDiagnostics) System.Diagnostics.Debug.WriteLine(s);
            var ls = (s + "").Split(_crlf, StringSplitOptions.None)
                .Select(x => ((EnableColors) ? $"{ColorSettings.Log}" : "") + x);
            Println(ls);
        }

        #endregion

        #region console operations
        
        delegate object CommandDelegate(object x);

        static readonly Dictionary<string, CommandDelegate> _drtvs = new Dictionary<string, CommandDelegate>() {
            { PrintDirectives.bkf+""   , (x) => RelayCall(BackupForeground) },
            { PrintDirectives.bkb+""   , (x) => RelayCall(BackupBackground) },
            { PrintDirectives.rsf+""   , (x) => RelayCall(RestoreForeground) },
            { PrintDirectives.rsb+""   , (x) => RelayCall(RestoreBackground) },
            { PrintDirectives.cl+""    , (x) => RelayCall(() => Clear()) },
            { PrintDirectives.f+"="    , (x) => RelayCall(() => SetForeground( TextColor.ParseColor(x))) },
            { PrintDirectives.b+"="    , (x) => RelayCall(() => SetBackground( TextColor.ParseColor(x))) },
            { PrintDirectives.df+"="   , (x) => RelayCall(() => SetDefaultForeground( TextColor.ParseColor(x))) },
            { PrintDirectives.db+"="   , (x) => RelayCall(() => SetDefaultBackground( TextColor.ParseColor(x))) },
            { PrintDirectives.br+""    , (x) => RelayCall(LineBreak) },
            { PrintDirectives.inf+""   , (x) => RelayCall(Infos) },
            { PrintDirectives.bkcr+""  , (x) => RelayCall(BackupCursorPos) },
            { PrintDirectives.rscr+""  , (x) => RelayCall(RestoreCursorPos) },
            { PrintDirectives.crh+""   , (x) => RelayCall(HideCur) },
            { PrintDirectives.crs+""   , (x) => RelayCall(ShowCur) },
            { PrintDirectives.crx+"="  , (x) => RelayCall(() => SetCursorLeft(GetCursorX(x))) },
            { PrintDirectives.cry+"="  , (x) => RelayCall(() => SetCursorTop(GetCursorY(x))) },
            { PrintDirectives.exit+""  , (x) => RelayCall(() => Exit()) },
            { PrintDirectives.exec+"=" , (x) => ExecCSharp((string)x) }
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
        public static void RestoreDefaultColors() => Lock(() => { sc.ForegroundColor = DefaultForeground; sc.BackgroundColor = DefaultBackground; });
        public static void Clear()
        {
            Lock(() =>
            {
                sc.Clear();
                RestoreDefaultColors();
                UpdateUI(true,false);
            }
            );
        }
        public static void LineBreak()
        {
            Lock(() =>
            {
                ConsolePrint(string.Empty, true);
            });
        }
        public static void Infos()
        {
            Lock(() =>
            {
                Println($"OS={Environment.OSVersion} {(Environment.Is64BitOperatingSystem ? "64" : "32")}bits");
                Println($"{White}{Bkf}{ColorSettings.HighlightIdentifier}window:{Rf} left={ColorSettings.Numeric}{sc.WindowLeft}{Rf},top={ColorSettings.Numeric}{sc.WindowTop}{Rf},width={ColorSettings.Numeric}{sc.WindowWidth}{Rf},height={ColorSettings.Numeric}{sc.WindowHeight}{Rf},largest width={ColorSettings.Numeric}{sc.LargestWindowWidth}{Rf},largest height={ColorSettings.Numeric}{sc.LargestWindowHeight}{Rf}");
                Println($"{ColorSettings.HighlightIdentifier}buffer:{Rf} width={ColorSettings.Numeric}{sc.BufferWidth}{Rf},height={ColorSettings.Numeric}{sc.BufferHeight}{Rf} | input encoding={ColorSettings.Numeric}{sc.InputEncoding.EncodingName}{Rf} | output encoding={ColorSettings.Numeric}{sc.OutputEncoding.EncodingName}{Rf}");
                Println($"number lock={ColorSettings.Numeric}{sc.NumberLock}{Rf} | capslock={ColorSettings.Numeric}{sc.CapsLock}{Rf}");            // TODO: not supported on linux ubuntu 18.04 wsl
                Println($"cursor visible={ColorSettings.Numeric}{sc.CursorVisible}{Rf} | cursor size={ColorSettings.Numeric}{sc.CursorSize}");     // TODO: not supported on linux ubuntu 18.04 wsl
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
        public static bool CursorVisible => sc.CursorVisible;
        public static void HideCur() => Lock(()=>sc.CursorVisible = false);
        public static void ShowCur() => Lock(()=>sc.CursorVisible = true);
        public static void Exit(int r=0) => Environment.Exit(r);

        public static void SetWorkArea(string id,int wx,int wy,int width,int height)
        {
            lock (ConsoleLock)
            {
                _workArea = (id,new Rectangle(wx, wy, width, height));
                ApplyWorkArea();
                EnableConstraintConsolePrintInsideWorkArea = true;
            }
        }
        public static void UnsetWorkArea()
        {
            _workArea = (null,Rectangle.Empty);
            EnableConstraintConsolePrintInsideWorkArea = false;
        }
        public static (string id,int left,int top,int right,int bottom) ActualWorkArea
        {
            get
            {
                var x0 = _workArea.rect.IsEmpty ? 0 : _workArea.rect.X;
                var y0 = _workArea.rect.IsEmpty ? 0 : _workArea.rect.Y;
                var w0 = _workArea.rect.IsEmpty ? -1 : _workArea.rect.Width;
                var h0 = _workArea.rect.IsEmpty ? -1 : _workArea.rect.Height;
                var (x, y, w, h) = GetCoords(x0, y0, w0, h0);
                return (_workArea.id,x,y,w,h);
            }
        }       
        static void ApplyWorkArea(bool viewSizeChanged=false)
        {
            lock (ConsoleLock)
            {
                if ( ViewResizeStrategy!=ViewResizeStrategy.HostTerminalDefault &&
                    (!viewSizeChanged ||
                    (viewSizeChanged && ViewResizeStrategy==ViewResizeStrategy.FitViewSize)))
                    try
                    {
                        sc.WindowTop = 0;
                        sc.WindowLeft = 0;
                        sc.BufferWidth = sc.WindowWidth;
                        sc.BufferHeight = sc.WindowHeight;
                    }
                    catch (Exception) { }
                if (_workArea.rect.IsEmpty) return;

            }
        }
        public static void SetCursorAtBeginWorkArea()
        {
            if (_workArea.rect.IsEmpty) return;
            lock (ConsoleLock) {
                SetCursorPos(_workArea.rect.X, _workArea.rect.Y);
            }
        }

        public static void Println(IEnumerable<string> ls) { foreach (var s in ls) Println(s); }
        public static void Print(IEnumerable<string> ls) { foreach (var s in ls) Print(s); }
        public static void Println(string s="") => Print(s, true);
        public static void Print(string s="") => Print(s, false);

        public static void Error(string s="") => Error(s, false);
        public static void Errorln(string s="") => Error(s, true);
        public static void Errorln(IEnumerable<string> ls) { foreach (var s in ls) Errorln(s); }
        public static void Error(IEnumerable<string> ls) { foreach (var s in ls) Error(s); }
        public static void Error(string s,bool lineBreak=false)
        {
            lock (ConsoleLock)
            {
                RedirectOutToError = true;
                Print($"{ColorSettings.Error}{s}", lineBreak);
                RedirectOutToError = false;
            }
        }

        public static void Warning(string s = "") => Warning(s, false);
        public static void Warningln(string s = "") => Warning(s, true);
        public static void Warningln(IEnumerable<string> ls) { foreach (var s in ls) Errorln(s); }
        public static void Warning(IEnumerable<string> ls) { foreach (var s in ls) Error(s); }
        public static void Warning(string s, bool lineBreak = false)
        {
            lock (ConsoleLock)
            {
                Print($"{ColorSettings.Warning}{s}", lineBreak);
            }
        }

        public static string GetPrint(string s,bool lineBreak=false)
        {
            lock (ConsoleLock)
            {
                var ms = new MemoryStream(s.Length * 4);
                var sw = new StreamWriter(ms);
                RedirectOutputTo(sw);
                var e = EnableConstraintConsolePrintInsideWorkArea;
                EnableConstraintConsolePrintInsideWorkArea = false;
                Print(s, lineBreak);
                EnableConstraintConsolePrintInsideWorkArea = e;
                sw.Flush();
                ms.Position = 0;
                var rw = new StreamReader(ms);
                var txt = rw.ReadToEnd();
                rw.Close();
                RedirectOutputTo(null);
                return txt;
            }
        }

        public static string Readln(string prompt = null)
        {
            lock (ConsoleLock)
            {
                if (prompt != null) Print(prompt);
            }
            return sc.ReadLine();
        }

        public static bool Confirm(string question)
        {
            var r = false;
            void endReadln(IAsyncResult result)
            {
                r = result.AsyncState?.ToString()?.ToLower() == "y";
            }
            var cmdlr = new CommandLineReader(question+"? ", null);
            cmdlr.BeginReadln(endReadln,null,true,false);
            Println();
            return r;
        }

        public static void Echo(string s,bool lineBreak=false,[CallerMemberName]string callerMemberName="",[CallerLineNumber]int callerLineNumber=-1)
        {
            if (!FileEchoEnabled) return;
            if (FileEchoDumpDebugInfo)
                _echoStreamWriter?.Write($"x={CursorLeft},y={CursorTop},l={s.Length},w={sc.WindowWidth},h={sc.WindowHeight},wtop={sc.WindowTop} bw={sc.BufferWidth},bh={sc.BufferHeight},br={lineBreak} [{callerMemberName}:{callerLineNumber}] :");
            _echoStreamWriter?.Write(s);
            if (lineBreak | FileEchoAutoLineBreak) _echoStreamWriter?.WriteLine(string.Empty);
            if (FileEchoAutoFlush) _echoStreamWriter?.Flush();
        }

        public static void EchoOn(
            string filepath,
            bool autoFlush=true,
            bool autoLineBreak=true,
            bool echoCommands=true,
            bool echoDebugInfo=false)
        {
            if (!string.IsNullOrWhiteSpace(filepath) && _echoFileStream == null)
            {
                FileEchoAutoFlush = autoFlush;
                FileEchoAutoLineBreak = autoLineBreak;
                FileEchoCommands = echoCommands;
                FileEchoDumpDebugInfo = echoDebugInfo;
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

        public static void ConsolePrint(string s, bool lineBreak = false)
        {
            // any print goes here...
            lock (ConsoleLock)
            {
                if (CropX==-1)
                    ConsoleSubPrint(s,lineBreak);
                else
                {
                    var x = CursorLeft;
                    var mx = Math.Max(x, CropX);
                    if (mx>x)
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

        static void ConsoleSubPrint(string s,bool lineBreak = false)
        {
            lock (ConsoleLock)
            {
                var (id,x, y, w, h) = ActualWorkArea;
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
                            Echo(line);
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
                        Echo(s);
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
                    Write(s);
                    Echo(s);
                    if (lineBreak)
                    {
                        sc.WriteLine(string.Empty);
                        Echo(string.Empty,true);
                    }
                }
            }
        }

        static void Write(string s)
        {
            if (RedirectOutToError)
                sc.Error.Write(s);
            else
                sc.Write(s);
        }

        public static int GetIndexInWorkAreaConstraintedString(string s, Point origin, Point cursorPos)
            => GetIndexInWorkAreaConstraintedString(s, origin, cursorPos.X, cursorPos.Y);

        public static int GetIndexInWorkAreaConstraintedString(string s,Point origin,int cursorX,int cursorY)
        {
            lock (ConsoleLock)
            {
                int index = -1;
                var (id,x, y, w, h) = ActualWorkArea;
                var x0 = origin.X;
                var y0 = origin.Y;

                var croppedLines = new List<string>();
                var xr = x0 + s.Length - 1;
                var xm = x + w - 1;
                if (xr >= xm)
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
                    int lineIndex = 0;
                    index = 0;
                    foreach (var line in croppedLines)
                    {
                        if (cursorY == y0)
                        {
                            index += cursorX - x0;
                            break;
                        }
                        x0 += line.Length;
                        index += line.Length;
                        SetCursorPosConstraintedInWorkArea(ref x0, ref y0,false);
                        lineIndex++;
                    }
                }
                else
                    return cursorX - x0;
                return index;
            }
        }

        public static List<(string s,int x,int y,int l)> GetWorkAreaStringSplits(string s, Point origin)
        {
            var r = new List<(string, int,int, int)>();
            lock (ConsoleLock)
            {
                int index = -1;
                var (id,x, y, w, h) = ActualWorkArea;
                var x0 = origin.X;
                var y0 = origin.Y;

                var croppedLines = new List<string>();
                var xr = x0 + s.Length - 1;
                var xm = x + w - 1;
                if (xr >= xm)
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
                    int lineIndex = 0;
                    index = 0;
                    foreach (var line in croppedLines)
                    {
                        r.Add((line,x0,y0,line.Length));                        
                        x0 += line.Length;
                        index += line.Length;
                        SetCursorPosConstraintedInWorkArea(ref x0, ref y0, false);
                        lineIndex++;
                    }
                }
                else
                    r.Add((s,x0,y0,s.Length));
            }
            return r;
        }

        public static void SetCursorPosConstraintedInWorkArea(Point pos, bool enableOutput = true)
        {
            var x = pos.X;
            var y = pos.Y;
            SetCursorPosConstraintedInWorkArea(ref x, ref y, enableOutput);            
        }

        public static void SetCursorPosConstraintedInWorkArea(int cx,int cy, bool enableOutput = true)
            => SetCursorPosConstraintedInWorkArea(ref cx, ref cy, enableOutput);

        public static void SetCursorPosConstraintedInWorkArea(ref int cx,ref int cy,bool enableOutput=true)
        {
            lock (ConsoleLock)
            {
                int dx = 0;
                int dy = 0;

                if (EnableConstraintConsolePrintInsideWorkArea)
                {
                    var (id,left, top, right, bottom) = ActualWorkArea;
                    if (cx<left)
                    {
                        cx = right - 1;
                        cy--;
                    }
                    if (cx>=right)
                    {
                        cx = left;
                        cy++;
                    }

                    if (enableOutput && cy < top)
                    {
                        dy = top-cy;
                        cy += dy;
                        if (top+1<=bottom)
                            sc.MoveBufferArea(      // TODO: not supported on linux (ubuntu 18.04 wsl)
                                left,top,right,bottom-top,
                                left,top+1,
                                ' ',
                                DefaultForeground, DefaultBackground);
                    }

                    if (enableOutput && cy > bottom /*- 1*/)
                    {
                        dy = bottom /*- 1*/ - cy;
                        cy+=dy;
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
            lock (ConsoleLock)
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

        static void UpdateUI(bool viewSizeChanged=false,bool enableViewSizeChangedEvent=true)
        {
            lock (ConsoleLock)
            {
                if (RedrawUIElementsEnabled)
                {
                    RedrawUIElementsEnabled = false;
                    var cursorPosBackup = CursorPos;

                    if (ViewResizeStrategy == ViewResizeStrategy.FitViewSize
                        && viewSizeChanged)
                    {
                        if (ClearOnViewResized)
                            Clear();
                    }

                    foreach (var o in _uielements)
                        o.Value.UpdateDraw(viewSizeChanged);

                    if (viewSizeChanged)
                    {
                        ApplyWorkArea(viewSizeChanged);
                        if (ViewResizeStrategy == ViewResizeStrategy.FitViewSize
                            && ClearOnViewResized)
                            if (_workArea.rect.IsEmpty)
                                SetCursorPos(cursorPosBackup);
                            else
                                SetCursorAtBeginWorkArea();
                        if (enableViewSizeChangedEvent)
                            ViewSizeChanged?.Invoke(null,EventArgs.Empty);
                    }

                    RedrawUIElementsEnabled = true;
                }
            }
        }

        static void EraseUIElements()
        {
            lock (ConsoleLock)
            {
                if (RedrawUIElementsEnabled)
                    foreach (var o in _uielements)
                        o.Value.Erase();
            }
        }

        #endregion
        
        #region stream methods

        public static void RedirectOutputTo(StreamWriter sw)
        {
            if (sw != null)
            {
                _outputWriter = sc.Out;
                _outputStreamWriter = sw;
                sc.SetOut(_outputStreamWriter);
            } else
            {
                _outputStreamWriter.Flush();
                _outputStreamWriter.Close();
                sc.SetOut(_outputWriter);
            }
        }

        public static void RedirectOutput(string filepath = null)
        {
            if (filepath!=null)
            {
                _outputWriter = sc.Out;
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

        #endregion

        #region disk operations

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

        static void TraceError(string s) => LogError(s);
        
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

        static void Print(object s, bool lineBreak = false, bool preserveColors = false, bool parseCommands = true)
        {
            lock (ConsoleLock)
            {
                var redrawUIElementsEnabled = RedrawUIElementsEnabled;
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

                RedrawUIElementsEnabled = redrawUIElementsEnabled;
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
#pragma warning disable IDE0057
                                value = s.Substring(firstCommandEndIndex, k - firstCommandEndIndex);
#pragma warning restore IDE0057
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
                    if (FileEchoEnabled && FileEchoCommands)
                        Echo(CommandBlockBeginChar + cmd.Value.Key + value + CommandBlockEndChar);
                }
                else
                {
                    result = cmd.Value.Value(null);
                    if (FileEchoEnabled && FileEchoCommands)
                        Echo(CommandBlockBeginChar + cmd.Value.Key + CommandBlockEndChar);
                }
                if (result != null)
                    Print(result, false);

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

        public static string DefaultBackgroundCmd => GetCmd(PrintDirectives.b + "", DefaultBackground.ToString().ToLower());
        public static string DefaultForegroundCmd => GetCmd(PrintDirectives.f + "", DefaultForeground.ToString().ToLower());

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
        public static string Cl => GetCmd(PrintDirectives.cl );
        public static string Br => GetCmd(PrintDirectives.br );

        public static string B(ConsoleColor c) => GetCmd(PrintDirectives.b , c+"");
        public static string F(ConsoleColor c) => GetCmd(PrintDirectives.f , c+"");

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
