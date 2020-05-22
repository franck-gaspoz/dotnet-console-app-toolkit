//#define dbg

using DotNetConsoleSdk.Component.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using static DotNetConsoleSdk.DotNetConsoleSdk;
using sc = System.Console;

namespace DotNetConsoleSdk.Component.Shell
{
    public static class ShellSdk
    {
        #region attributes

        static Thread _inputReaderThread;
        static readonly List<string> _history = new List<string>();
        static int _historyIndex = -1;
        static string _prompt;
        static StringBuilder _inputReaderStringBuilder;

        #endregion

        static void InitUI()
        {
            Clear();
            SetWorkArea(0, 4, -1, 10);

            AddFrame((bar) =>
            {
                var s = "".PadLeft(bar.ActualWidth, '-');
                var t = "  dotnet-console-sdk - Shell Sdk";
                return new List<string> {
                        $"{Bdarkblue}{Cyan}{s}",
#pragma warning disable IDE0071
#pragma warning disable IDE0071WithoutSuggestion
                        $"{Bdarkblue}{Cyan}|{t}{White}{"".PadLeft(Math.Max(0, bar.ActualWidth - 2 - t.Length))}{Cyan}|",
#pragma warning restore IDE0071WithoutSuggestion
#pragma warning restore IDE0071
                        $"{Bdarkblue}{Cyan}{s}"
                    };
            }, ConsoleColor.DarkBlue, 0, 0, -1, 3, DrawStrategy.OnViewResizedOnly, false);

            string GetCurrentDriveInfo()
            {
                var rootDirectory = Path.GetPathRoot(Environment.CurrentDirectory);
                var di = DriveInfo.GetDrives().Where(x => x.RootDirectory.FullName == rootDirectory).FirstOrDefault();
                return (di==null)?"?":$"{rootDirectory} {HumanFormatOfSize(di.AvailableFreeSpace,0,"")}/{HumanFormatOfSize(di.TotalSize,0,"")} ({di.DriveFormat})";
            }

            AddFrame((bar) =>
            {
                return new List<string> {
                        $"{Bdarkblue} {Green}cur: {Cyan}{CursorLeft},{CursorTop}{White}"
                        +$" | {Green}win: {Cyan}{sc.WindowLeft},{sc.WindowTop}"
                        +$",{sc.WindowWidth},{sc.WindowHeight}{White}"
                        +$" | {(sc.CapsLock?$"{Cyan}Caps":$"{Darkgray}Caps")}"
                        +$" {(sc.NumberLock?$"{Cyan}Num":$"{Darkgray}Num")}{White}"
                        +$" | {Green}in={Cyan}{sc.InputEncoding.CodePage}"
                        +$" {Green}out={Cyan}{sc.OutputEncoding.CodePage}{White}"
                        +$" | {Green}drive: {Cyan}{GetCurrentDriveInfo()}{White}"
                        +$" | {Cyan}{System.DateTime.Now}{White}"
                    };
            }, ConsoleColor.DarkBlue, 0, -1, -1, 1, DrawStrategy.OnPrint, false, 1000);

            SetCursorAtBeginWorkArea();
            Infos();
            LineBreak();
        }

        static void Init()
        {
            ViewSizeChanged += (o, e) =>
            {
                if (_inputReaderThread != null)
                {
                    lock (ConsoleLock)
                    {
                        Print(_prompt);
                        ConsolePrint(_inputReaderStringBuilder.ToString());
                    }
                }
            };
        }

        public static void RunShell(string prompt = null)
        {
            try
            {
                Init();
                InitUI();
                BeginReadln(new AsyncCallback(ProcessInput),prompt);
            }
            catch (Exception initException)
            {
                LogError(initException);
                Exit();
            }
        }

        static void ProcessInput(IAsyncResult asyncResult)
        {
            var s = (string)asyncResult.AsyncState;
            if (s != null)
            {
                lock (ConsoleLock)
                {
                    LineBreak();
                    Println(s);
                }
                if (!string.IsNullOrWhiteSpace(s))
                    HistoryAppend(s);
            }
        }

        public static void BeginReadln(AsyncCallback asyncCallback, string prompt = "")
        {
            _prompt = prompt;            
            _inputReaderThread = new Thread(() =>
            {
                try
                {
                    while (true)
                    {
                        _inputReaderStringBuilder = new StringBuilder();
                        Point beginOfLineCurPos;
                        lock (ConsoleLock)
                        {
                            Print(prompt);
                            beginOfLineCurPos = CursorPos;
                        }
                        var eol = false;
                        while (!eol)
                        {
                            var c = sc.ReadKey(true);
#if dbg
                            System.Diagnostics.Debug.WriteLine($"{c.KeyChar}={c.Key}");
#endif

                            #region handle special caracters - edition mode, movement

                            var printed = false;
                            string printedStr = "";
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
                                        Print("".PadLeft(_inputReaderStringBuilder.ToString().Length));
                                        SetCursorPos(beginOfLineCurPos);
                                        ShowCur();
                                        _inputReaderStringBuilder.Clear();
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
                                        SetCursorLeft(_inputReaderStringBuilder.ToString().Length + beginOfLineCurPos.X);
                                    }
                                    break;
                                case ConsoleKey.Tab:
                                    lock (ConsoleLock)
                                    {
                                        printedStr = "".PadLeft(7, ' ');
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
                                        if (x < beginOfLineCurPos.X + _inputReaderStringBuilder.ToString().Length)
                                            SetCursorLeft(x + 1);
                                    }
                                    break;
                                case ConsoleKey.Backspace:
                                    lock (ConsoleLock)
                                    {
                                        var x = CursorLeft;
                                        if (x > beginOfLineCurPos.X)
                                        {
                                            var x0 = x - beginOfLineCurPos.X - 1;
                                            _inputReaderStringBuilder.Remove(x0, 1);
                                            HideCur();
                                            SetCursorLeft(x - 1);
                                            var txt = _inputReaderStringBuilder.ToString();
                                            if (x0 < txt.Length)
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
                                        var txt = _inputReaderStringBuilder.ToString();
                                        var x0 = x - beginOfLineCurPos.X;
                                        if (x0 < txt.Length)
                                        {
                                            _inputReaderStringBuilder.Remove(x0, 1);
                                            txt = _inputReaderStringBuilder.ToString();
                                            HideCur();
                                            if (x0 < txt.Length)
                                                Print(txt.Substring(x0) + " ");
                                            else
                                                Print(" ");
                                            SetCursorLeft(x);
                                            ShowCur();
                                        }
                                    }
                                    break;
                                case ConsoleKey.UpArrow:
                                    var h = GetBackwardHistory();
                                    if (h != null)
                                    {
                                        lock (ConsoleLock)
                                        {
                                            HideCur();
                                            SetCursorLeft(beginOfLineCurPos.X);
                                            Print("".PadLeft(_inputReaderStringBuilder.ToString().Length, ' '));
                                            SetCursorLeft(beginOfLineCurPos.X);
                                            _inputReaderStringBuilder.Clear();
                                            _inputReaderStringBuilder.Append(h);
                                            ConsolePrint(h);
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
                                            Print("".PadLeft(_inputReaderStringBuilder.ToString().Length, ' '));
                                            SetCursorLeft(beginOfLineCurPos.X);
                                            _inputReaderStringBuilder.Clear();
                                            _inputReaderStringBuilder.Append(fh);
                                            ConsolePrint(fh);
                                            ShowCur();
                                        }
                                    }
                                    break;
                                default:
                                    printedStr = c.KeyChar + "";
                                    printed = true;
                                    break;
                            }

                            #endregion

                            if (printed)
                            {
                                int x = 0;
                                var insert = false;
                                lock (ConsoleLock)
                                {
                                    var x0 = CursorLeft;
                                    x = x0 - beginOfLineCurPos.X;
                                    var txt = _inputReaderStringBuilder.ToString();
                                    insert = x - txt.Length < 0;
                                    if (insert)
                                    {
                                        var substr = txt.Substring(x);
                                        HideCur();
                                        SetCursorLeft(x0 + printedStr.Length);
                                        ConsolePrint(substr);
                                        SetCursorLeft(x0);
                                        ShowCur();
                                    }
                                    ConsolePrint(printedStr, false);
                                }
                                if (!insert)
                                    _inputReaderStringBuilder.Append(printedStr);
                                else
                                    _inputReaderStringBuilder.Insert(x, printedStr);
                            }

                            if (eol) break;
                        }

                        // process input
                        var s = _inputReaderStringBuilder.ToString();
                        asyncCallback?.Invoke(
                            new BeginReadlnAsyncResult(s)
                            );
                    }
                }
                catch (ThreadInterruptedException) { }
                catch (Exception ex)
                {
                    LogError("input stream reader crashed: " + ex.Message);
                }
            })
            {
                Name = "input stream reader"
            };
            _inputReaderThread.Start();
        }

        public static void StopBeginReadln()
        {
            _inputReaderThread?.Interrupt();
            _inputReaderThread = null;
        }

        #region shell control operations

        public static string GetBackwardHistory()
        {
            if (_historyIndex < 0)
                _historyIndex = _history.Count;
            if (_historyIndex >= 1)
                _historyIndex--;
            System.Diagnostics.Debug.WriteLine($"{_historyIndex}");
            return (_historyIndex < 0 || _history.Count == 0 || _historyIndex >= _history.Count) ? null : _history[_historyIndex];
        }

        public static string GetForwardHistory()
        {
            if (_historyIndex < 0 || _historyIndex >= _history.Count)
                _historyIndex = _history.Count;
            if (_historyIndex < _history.Count - 1) _historyIndex++;

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
    }
}
