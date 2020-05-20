#define dbg

using DotNetConsoleSdk.Component;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Threading;
using sc = System.Console;
using static DotNetConsoleSdk.Component.UIElement;
using Microsoft.VisualBasic.CompilerServices;
using System.Reflection;
using System.IO;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

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
        public static bool DumpExceptions = true;
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

        static Thread _watcherThread;
        static Dictionary<int, UIElement> _uielements = new Dictionary<int, UIElement>();

        static string[] _args;
        static TextWriter _outputWriter;
        static StreamWriter _outputStreamWriter;
        static FileStream _outputFileStream;
        static StreamWriter _echoStreamWriter;
        static FileStream _echoFileStream;

        static Dictionary<string, Script<object>> _csscripts = new Dictionary<string, Script<object>>();

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
            { KeyWords.cl+""    , (x) => RelayCall(Clear) },
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
            { KeyWords.exit+""  , (x) => RelayCall(() => Environment.Exit(0)) },
            { KeyWords.exec+"=" , (x) => ExecCSharp((string)x) }
        };

        static object RelayCall(Action method) { method(); return null; }

        public static void BackupForeground() => _foregroundBackup = sc.ForegroundColor;
        public static void BackupBackground() => _backgroundBackup = sc.BackgroundColor;
        public static void RestoreForeground() => sc.ForegroundColor = _foregroundBackup;
        public static void RestoreBackground() => sc.BackgroundColor = _backgroundBackup;
        public static void SetForeground(ConsoleColor c) => sc.ForegroundColor = c;
        public static void SetBackground(ConsoleColor c) => sc.BackgroundColor = c;
        public static void SetDefaultForeground(ConsoleColor c) => DefaultForeground = c;
        public static void SetDefaultBackground(ConsoleColor c) => DefaultBackground = c;
        public static void Clear()
        {
            sc.Clear();
            RedrawUIElements();
        }
        public static void LineBreak()
        {
            ConsolePrint(string.Empty, true);
            RedrawUIElements();
        }
        public static void Infos()
        {
            Println($"{White}{Bkf}{Green}window:{Rf} left={Cyan}{Console.WindowLeft}{Rf},top={Cyan}{Console.WindowTop}{Rf},width={Cyan}{Console.WindowWidth}{Rf},height={Cyan}{Console.WindowHeight}{Rf},largest width={Cyan}{Console.LargestWindowWidth}{Rf},largest height={Cyan}{Console.LargestWindowHeight}{Rf}");
            Println($"{Green}buffer:{Rf} width={Cyan}{Console.BufferWidth}{Rf},height={Cyan}{Console.BufferHeight}{Rf} | input encoding={Cyan}{Console.InputEncoding.EncodingName}{Rf} | output encoding={Cyan}{Console.OutputEncoding.EncodingName}{Rf}");
            Println($"number lock={Cyan}{Console.NumberLock}{Rf} | capslock={Cyan}{Console.CapsLock}{Rf} | cursor visible={Cyan}{Console.CursorVisible}{Rf} | cursor size={Cyan}{Console.CursorSize}");
        }
        public static void BackupCursorPos()
        {
            _cursorLeftBackup = Console.CursorLeft;
            _cursorTopBackup = Console.CursorTop;
        }
        public static void RestoreCursorPos()
        {
            Console.CursorLeft = _cursorLeftBackup;
            Console.CursorTop = _cursorTopBackup;
        }
        public static void SetCursorLeft(int x) => Console.CursorLeft = FixX(x);
        public static void SetCursorTop(int y) => Console.CursorTop = FixY(y);
        public static int CursorLeft => Console.CursorLeft;
        public static int CursorTop => Console.CursorTop;
        public static Point CursorPos => new Point(CursorLeft, CursorTop);
        public static void SetCursorPos(Point p)
        {
            var x = p.X;
            var y = p.Y;
            FixCoords(ref x, ref y);
            Console.CursorLeft = x;
            Console.CursorTop = y;
        }
        public static void SetCursorPos(int x,int y)
        {
            FixCoords(ref x, ref y);
            Console.CursorLeft = x;
            Console.CursorTop = y;
        }
        public static void HideCur() => Console.CursorVisible = false;
        public static void ShowCur() => Console.CursorVisible = true;

        public static void Println(IEnumerable<string> ls) { foreach (var s in ls) Println(s); }
        public static void Print(IEnumerable<string> ls) { foreach (var s in ls) Print(s); }
        public static void Println(string s) => Print(s, true);
        public static void Print(string s) => Print(s, false);

        public static string Readln(string prompt = null)
        {
            if (prompt != null) Print(prompt);
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
                LogError(string.Join(Environment.NewLine,ex.Diagnostics));
                return null;
            }
        }

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

        public static void RunUIElementWatcher()
        {
            if (_watcherThread != null)
                return;
            int lastWinHeight = Console.WindowHeight;
            int lastWinWidth = Console.WindowWidth;
            int lastWinTop = Console.WindowTop;
            int lastWinLeft = Console.WindowLeft;
            _watcherThread = new Thread(() =>
            {
                try
                {
                    while (true)
                    {
                        var h = Console.WindowHeight;
                        var w = Console.WindowWidth;
                        var l = Console.WindowLeft;
                        var t = Console.WindowTop;
                        if (w != lastWinWidth || h != lastWinHeight || l != lastWinLeft || t != lastWinTop)
                            RedrawUIElements(true);
                        lastWinHeight = h;
                        lastWinWidth = w;
                        lastWinLeft = l;
                        lastWinTop = t;
                        Thread.Sleep(500);
                    }
                }
                catch (ThreadInterruptedException) { }
            });
            _watcherThread.Name = "console tool watcher";
            _watcherThread.Start();
        }

        public static int AddFrame(Func<Frame,string> printContent, ConsoleColor backgroundColor, int x = 0, int y = -1, int w = -1, int h = 1, DrawStrategy drawStrategy = DrawStrategy.OnViewResized, bool mustRedrawBackground = true)
        {
            var o = new Frame(printContent, backgroundColor, x, y, w, h, drawStrategy, mustRedrawBackground);
            o.Draw();
            _uielements.Add(o.Id,o);            
            RunUIElementWatcher();
            return o.Id;
        }

        public static bool RemoveFrame(int id)
        {
            if (_uielements.ContainsKey(id))
            {
                _uielements.Remove(id);
                return true;
            }
            return false;
        }

        static void RedrawUIElements(bool forceDraw = false)
        {
            if (_redrawUIElementsEnabled && _uielements.Count>0)
            {
                _redrawUIElementsEnabled = false;
                if (ClearOnViewResized && forceDraw)
                {
                    Clear();
                }
                foreach (var o in _uielements)
                    o.Value.UpdateDraw(forceDraw & !ClearOnViewResized,forceDraw);
                _redrawUIElementsEnabled = true;
            }
        }

        static void EraseUIElements()
        {
            if (_redrawUIElementsEnabled)
                foreach (var o in _uielements)
                    o.Value.Erase();
        }

        #endregion

        #region cli methods

        public static void RunSampleCLI(string prompt = null)
        {
            Clear();

            AddFrame((bar) =>
            {
                var s = "".PadLeft(bar.ActualWidth, '-');
                var t = "  dotnet console sdk - sample CLI";
                var r = $"{Bdarkblue}{Cyan}{s}{Br}";
                r += $"{Bdarkblue}{Cyan}|{t}{White}{"".PadLeft(bar.ActualWidth - 2 - t.Length)}{Cyan}|{Br}";
                r += $"{Bdarkblue}{Cyan}{s}{Br}";
                return r;
            }, ConsoleColor.DarkBlue, 0, 0, -1, 3,DrawStrategy.OnViewResized, false);

            SetCursorPos(0, 4);
            Infos();
            LineBreak();

            AddFrame((bar) =>
            {
                var r = $"{Bdarkblue}{Green}Cursor pos: {White}X={Cyan}{CursorLeft}{Green},{White}Y={Cyan}{CursorTop}{White}";
                r += $" | {Green}bar pos: {White}X={Cyan}{bar.ActualX}{Green},{White}Y={Cyan}{bar.ActualY}{White}";
                return r;
            }, ConsoleColor.DarkBlue);

            var end = false;
            while (!end)
            {
                var s = Readln(prompt);                
                Println(s);
            }
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
            return Console.CursorLeft;
        }
        static int GetCursorY(object x)
        {
            if (x != null && x is string s && !string.IsNullOrWhiteSpace(s)
                && int.TryParse(s, out var v))
                return v;
            if (TraceCommandErrors) TraceError($"wrong cursor y: {x}");
            return Console.CursorTop;
        }

        static void ConsolePrint(string s, bool lineBreak = false)
        {
            sc.Write(s);
            if (lineBreak) sc.WriteLine(string.Empty);
        }

        static void Print(object s, bool lineBreak = false, bool parseCommands = true)
        {
            var redrawUIElementsEnabled = _redrawUIElementsEnabled;
            _redrawUIElementsEnabled = false;
            if (SaveColors)
            {
                BackupBackground();
                BackupForeground();
            }

            if (EnableColors)
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

            if (SaveColors)
            {
                RestoreBackground();
                RestoreForeground();
            }

            if (lineBreak) LineBreak();

            _redrawUIElementsEnabled = redrawUIElementsEnabled;
            RedrawUIElements();
        }

        static void ParseTextAndApplyCommands(string s, bool lineBreak = false, string tmps = "")
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
                firstCommandEndIndex = s.IndexOf(CommandValueAssignationChar, i+1);
                if (firstCommandEndIndex>-1)
                {
                    firstCommandEndIndex++;
                    var subs = s.Substring(firstCommandEndIndex);
                    if (subs.StartsWith(CodeBlockBegin))
                    {
                        firstCommandEndIndex += CodeBlockBegin.Length;
                        k = s.IndexOf(CodeBlockEnd,firstCommandEndIndex);
                        if (k>-1)
                        {
                            value = s.Substring(firstCommandEndIndex, k - firstCommandEndIndex);
                            k += CodeBlockEnd.Length;
                        } else
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
            while (j<s.Length)
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
            if (j==s.Length)
            {
                ConsolePrint(s);
                return;
            }

            var cmdtxt = s[i..j];
            if (firstCommandSeparatorCharIndex > -1)
                cmdtxt = cmdtxt.Substring(0, firstCommandSeparatorCharIndex-1);

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
                Print(result);

            if (firstCommandSeparatorCharIndex > -1)
                s = CommandBlockBeginChar + s.Substring(k + i );
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
