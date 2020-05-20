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
         */

        static readonly Dictionary<string, Action<object>> _drtvs = new Dictionary<string, Action<object>>() {
            { "bkf" , (x) => BackupForeground() },
            { "bkb" , (x) => BackupBackground() },
            { "rsf" , (x) => RestoreForeground() },
            { "rsb" , (x) => RestoreBackground() },
            { "cl" , (x) => Clear() },
            { "f=" , (x) => SetForeground( ParseColor(x)) },
            { "b=" , (x) => SetBackground( ParseColor(x)) },
            { "df=" , (x) => SetDefaultForeground( ParseColor(x)) },
            { "db=" , (x) => SetDefaultBackground( ParseColor(x)) },
            { "br" , (x) => LineBreak() },
            { "inf" , (x) => Infos() },
            { "bkcr" , (x) => BackupCursorPos() },
            { "rscr" , (x) => RestoreCursorPos() },
            { "crh" , (x) => HideCur() },
            { "crs" , (x) => ShowCur() },
            { "crx=" , (x) => SetCursorLeft(GetCursorX(x)) },
            { "cry=" , (x) => SetCursorTop(GetCursorY(x)) },
            { "exit" , (x) => Environment.Exit(0) }
        };

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
            if (_redrawUIElementsEnabled)
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
                var t = "  Console tool: commands tester  ";
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
            KeyValuePair<string, Action<object>>? cmd = null;
            int n = s.Length;
            while (cmd == null && i < n)
            {
                foreach (var ccmd in _drtvs)
                {
                    if (s.IndexOf(CommandBlockBeginChar + ccmd.Key, i) == i)
                    {
                        cmd = ccmd;
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

            var j = s.IndexOf(CommandBlockEndChar, i + 1);
            if (j==-1)
            {
                ConsolePrint(s);
                return;
            }

            var cmdtxt = s[i..j];
            var k = cmdtxt.IndexOf(CommandSeparatorChar);
            if (k > -1)
                cmdtxt = cmdtxt.Substring(0, k);
            if (cmd.Value.Key.EndsWith(CommandValueAssignationChar))
            {
                var t = cmdtxt.Split(CommandValueAssignationChar);
                var value = t[1];
                cmd.Value.Value(value);
            } else
                cmd.Value.Value(null);

            if (k > -1)
                s = CommandBlockBeginChar + s.Substring(k + i + 1);
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

        public static string Bblack => GetCmd("b", "black");
        public static string Bdarkblue => GetCmd("b", "darkblue");
        public static string Bdarkgreen => GetCmd("b", "darkgreen");
        public static string Bdarkcyan => GetCmd("b", "darkcyan");
        public static string Bdarkred => GetCmd("b", "darkred");
        public static string Bdarkmagenta => GetCmd("b", "darkmagenta");
        public static string Bdarkyellow => GetCmd("b", "darkyelllow");
        public static string Bgray => GetCmd("b", "gray");
        public static string Bdarkgray => GetCmd("b", "darkgray");
        public static string Bblue => GetCmd("b", "blue");
        public static string Bgreen => GetCmd("b", "green");
        public static string Bcyan => GetCmd("b", "cyan");
        public static string Bred => GetCmd("b", "red");
        public static string Bmagenta => GetCmd("b", "magenta");
        public static string Byellow => GetCmd("b", "yellow");
        public static string Bwhite => GetCmd("b", "white");
        public static string Black => GetCmd("f", "black");
        public static string Darkblue => GetCmd("f", "darkblue");
        public static string Darkgreen => GetCmd("f", "darkgreen");
        public static string Darkcyan => GetCmd("f", "darkcyan");
        public static string Darkred => GetCmd("f", "darkred");
        public static string Darkmagenta => GetCmd("f", "darkmagenta");
        public static string Darkyellow => GetCmd("f", "darkyelllow");
        public static string Gray => GetCmd("f", "gray");
        public static string Darkgray => GetCmd("f", "darkgray");
        public static string Blue => GetCmd("f", "blue");
        public static string Green => GetCmd("f", "green");
        public static string Cyan => GetCmd("f", "cyan");
        public static string Red => GetCmd("f", "red");
        public static string Magenta => GetCmd("f", "magenta");
        public static string Yellow => GetCmd("f", "yellow");
        public static string White => GetCmd("f", "white");

        public static string Bkf => GetCmd("bkf");
        public static string Rf => GetCmd("rsf");
        public static string Bkb => GetCmd("bkb");
        public static string Rb => GetCmd("rsb");
        public static string Cl => GetCmd("cl");
        public static string Br => GetCmd("br");

        public static string B(ConsoleColor c) => GetCmd("b", c+"");
        public static string F(ConsoleColor c) => GetCmd("f", c+"");

        public static string Bkcr => GetCmd("bkcr");
        public static string Rscr => GetCmd("rscr");
        public static string Crx(int x) => GetCmd("crx", x + "");
        public static string Cry(int y) => GetCmd("cry", y + "");
        public static string Cr(int x, int y) => $"{GetCmd("crx", x + "")}{GetCmd("cry", y + "")}";

        #endregion

    }
}
