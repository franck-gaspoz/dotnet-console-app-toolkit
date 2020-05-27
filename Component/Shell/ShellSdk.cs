//#define dbg

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

        static Thread _inputReaderThread;
        static readonly List<string> _history = new List<string>();
        static int _historyIndex = -1;
        static string _prompt;
        static StringBuilder _inputReaderStringBuilder;

        #endregion

        public static void Initialize()
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

        public static void ProcessInput(IAsyncResult asyncResult)
        {
            var s = (string)asyncResult.AsyncState;
            if (s != null)
            {
                lock (ConsoleLock)
                {
                    LineBreak();
                    Print(s);
                    if (!WorkArea.IsEmpty && (WorkArea.Y != CursorTop || WorkArea.X != CursorLeft))
                        LineBreak();
                    RestoreDefaultColors();
                }
                if (!string.IsNullOrWhiteSpace(s))
                    HistoryAppend(s);
            }
        }

        public static void BeginReadln(string prompt="")
        {
            BeginReadln(new AsyncCallback(ShellSdk.ProcessInput), prompt);
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

                            var (wx, wy, ww, wh) = ActualWorkArea;

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
                                        var p = CursorPos;
                                        if (p.Y == beginOfLineCurPos.Y)
                                        {
                                            if (p.X > beginOfLineCurPos.X)
                                                SetCursorLeft(p.X - 1);
                                        }
                                        else
                                        {
                                            var x = p.X - 1;
                                            if (x < wx)
                                                SetCursorPos(ww - 1, p.Y - 1);
                                            else
                                                SetCursorLeft(x);
                                        }
                                    }
                                    break;
                                case ConsoleKey.RightArrow:
                                    lock (ConsoleLock)
                                    {
                                        var txt = _inputReaderStringBuilder.ToString();
                                        var index = GetIndexInWorkAreaConstraintedString(txt, beginOfLineCurPos, CursorPos);
                                        if (index < txt.Length)
                                            SetCursorPosConstraintedInWorkArea(CursorLeft + 1, CursorTop);
                                    }
                                    break;
                                case ConsoleKey.Backspace:
                                    lock (ConsoleLock)
                                    {
                                        var txt = _inputReaderStringBuilder.ToString();
                                        var index = GetIndexInWorkAreaConstraintedString(txt, beginOfLineCurPos, CursorPos)-1;
                                        var x = CursorLeft-1;
                                        var y = CursorTop;
                                        if (index >= 0)
                                        {
                                            _inputReaderStringBuilder.Remove(index, 1);
                                            HideCur();
                                            SetCursorPosConstraintedInWorkArea(ref x, ref y);
                                            txt = _inputReaderStringBuilder.ToString();
                                            if (index < txt.Length)
                                                ConsolePrint(txt.Substring(index));
                                            ConsolePrint(" ");
                                            SetCursorPos(x,y);
                                            ShowCur();
                                        }
                                    }
                                    break;
                                case ConsoleKey.Delete:
                                    lock (ConsoleLock)
                                    {
                                        var txt = _inputReaderStringBuilder.ToString();
                                        var index = GetIndexInWorkAreaConstraintedString(txt, beginOfLineCurPos, CursorPos);
                                        var x = CursorLeft;
                                        var y = CursorTop;
                                        if (index >= 0 && index<txt.Length)
                                        {
                                            _inputReaderStringBuilder.Remove(index, 1);
                                            HideCur();
                                            SetCursorPosConstraintedInWorkArea(ref x, ref y);
                                            txt = _inputReaderStringBuilder.ToString();
                                            if (index < txt.Length)
                                                ConsolePrint(txt.Substring(index));
                                            ConsolePrint(" ");
                                            SetCursorPos(x, y);
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
                                            ConsolePrint("".PadLeft(_inputReaderStringBuilder.ToString().Length, ' '));
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
                                            ConsolePrint("".PadLeft(_inputReaderStringBuilder.ToString().Length, ' '));
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
                                var index = 0;
                                var insert = false;
                                lock (ConsoleLock)
                                {
                                    var x0 = CursorLeft;
                                    var y0 = CursorTop;
                                    //x = x0 - beginOfLineCurPos.X;
                                    var txt = _inputReaderStringBuilder.ToString();
                                    index = GetIndexInWorkAreaConstraintedString(txt, beginOfLineCurPos, x0, y0);
                                    insert = index - txt.Length < 0;

                                    System.Diagnostics.Debug.WriteLine($"cur={x0},{y0} index={index} insert={insert} txt={txt}");

                                    if (insert)
                                    {
                                        var substr = txt.Substring(index);
                                        HideCur();
                                        SetCursorPosConstraintedInWorkArea(x0 + printedStr.Length, y0);
                                        ConsolePrint(substr);
                                        SetCursorPos(x0,y0);
                                        ShowCur();
                                    }
                                    ConsolePrint(printedStr, false);

                                    //System.Diagnostics.Debug.WriteLine($"cur={CursorLeft},{CursorTop}");
                                }
                                if (!insert)
                                    _inputReaderStringBuilder.Append(printedStr);
                                else
                                    _inputReaderStringBuilder.Insert(index, printedStr);
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
                _historyIndex = _history.Count+1;
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
