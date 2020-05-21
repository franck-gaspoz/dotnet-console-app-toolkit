//#define dbg

using DotNetConsoleSdk.Component.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Threading;
using static DotNetConsoleSdk.DotNetConsoleSdk;
using sc = System.Console;

namespace DotNetConsoleSdk.Component.Shell
{
    public static class ShellSdk
    {
        #region attributes

        static Thread _readerThread;
        static readonly List<string> _history = new List<string>();
        static int _historyIndex = -1;

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
#pragma warning disable IDE0071 // Simplifier l’interpolation
#pragma warning disable IDE0071WithoutSuggestion // Simplifier l’interpolation
                        $"{Bdarkblue}{Cyan}|{t}{White}{"".PadLeft(Math.Max(0, bar.ActualWidth - 2 - t.Length))}{Cyan}|",
#pragma warning restore IDE0071WithoutSuggestion // Simplifier l’interpolation
#pragma warning restore IDE0071 // Simplifier l’interpolation
                        $"{Bdarkblue}{Cyan}{s}"
                    };
            }, ConsoleColor.DarkBlue, 0, 0, -1, 3, DrawStrategy.OnViewResizedOnly, false);

            AddFrame((bar) =>
            {
                return new List<string> {
                        $"{Bdarkblue} {Green}Cur: {Cyan}{CursorLeft},{CursorTop}{White}"
                        +$" | {Green}Win: {Cyan}{sc.WindowLeft},{sc.WindowTop}"
                        +$",{sc.WindowWidth},{sc.WindowHeight}{White}"
                        +$" | {(sc.CapsLock?$"{Cyan}Caps":$"{Darkgray}Caps")}{White}"
                        +$" | {(sc.NumberLock?$"{Cyan}Num":$"{Darkgray}Num")}{White}"
                        +$" | {Cyan}in={sc.InputEncoding.CodePage}{White}"
                        +$" | {Cyan}out={sc.OutputEncoding.CodePage}{White}"
                        +$" | {Cyan}{System.DateTime.Now}{White}"
                    };
            }, ConsoleColor.DarkBlue, 0, -1, -1, 1, DrawStrategy.OnTime, true, 1000);

            SetCursorPos(0, 4);
            Infos();
            LineBreak();
        }

        public static void RunShell(string prompt = null)
        {
            try
            {
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
            _readerThread = new Thread(() =>
            {
                try
                {
                    while (true)
                    {
                        var r = new StringBuilder();
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
                                            var x0 = x - beginOfLineCurPos.X - 1;
                                            r.Remove(x0, 1);
                                            HideCur();
                                            SetCursorLeft(x - 1);
                                            var txt = r.ToString();
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
                                        var txt = r.ToString();
                                        var x0 = x - beginOfLineCurPos.X;
                                        if (x0 < txt.Length)
                                        {
                                            r.Remove(x0, 1);
                                            txt = r.ToString();
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
                                            Print("".PadLeft(r.ToString().Length, ' '));
                                            SetCursorLeft(beginOfLineCurPos.X);
                                            r.Clear();
                                            r.Append(h);
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
                                            Print("".PadLeft(r.ToString().Length, ' '));
                                            SetCursorLeft(beginOfLineCurPos.X);
                                            r.Clear();
                                            r.Append(fh);
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
                                    var txt = r.ToString();
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
                                    r.Append(printedStr);
                                else
                                    r.Insert(x, printedStr);
                            }

                            if (eol) break;
                        }

                        // process input
                        var s = r.ToString();
                        asyncCallback?.Invoke(
                            new BeginReadlnAsyncResult(s)
                            );
                    }
                }
                catch { }
            })
            {
                Name = "input stream reader"
            };
            _readerThread.Start();
        }

        public static void StopBeginReadln()
        {
            _readerThread?.Interrupt();
            _readerThread = null;
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
