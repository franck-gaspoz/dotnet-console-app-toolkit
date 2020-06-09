//#define dbg

using DotNetConsoleSdk.Component.CommandLine;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static DotNetConsoleSdk.Component.CommandLine.CommandLineProcessor;
using static DotNetConsoleSdk.DotNetConsole;
using sc = System.Console;

namespace DotNetConsoleSdk.Component.CommandLine.CommandLineReader
{
    public static class CommandLineReader
    {
        #region attributes

        public delegate ExpressionEvaluationResult ExpressionEvaluationCommandDelegate(string com,int outputX);

        static Thread _inputReaderThread;
        static readonly List<string> _history = new List<string>();
        static int _historyIndex = -1;
        static string _prompt;
        static StringBuilder _inputReaderStringBuilder;
        static Point _beginOfLineCurPos;
        static ExpressionEvaluationCommandDelegate _evalCommandDelegate;
        static string _sentInput = null;
        static bool _waitForReaderExited;
        static bool _readingStarted;
        static string _nextPrompt = null;

        public static List<string> History => new List<string>(_history);
        public static Action<IAsyncResult> InputProcessor;

        #endregion

        public static void SetPrompt(string prompt)
        {
            _nextPrompt = prompt;
        }

        public static void InitializeCommandLineReader(ExpressionEvaluationCommandDelegate evalCommandDelegate)
        {
            _evalCommandDelegate = evalCommandDelegate ?? Eval;
            ViewSizeChanged += (o, e) =>
            {
                if (_inputReaderThread != null)
                {
                    lock (ConsoleLock)
                    {
                        Print(_prompt);
                        _beginOfLineCurPos = CursorPos;
                        ConsolePrint(_inputReaderStringBuilder.ToString());
                    }
                }
            };
            WorkAreaScrolled += (o, e) =>
            {
                if (_inputReaderThread != null)
                {
                    lock (ConsoleLock)
                    {
                        _beginOfLineCurPos.X += e.DeltaX;
                        _beginOfLineCurPos.Y += e.DeltaY;
                        var p = CursorPos;
                        var (id,left, top, right, bottom) = ActualWorkArea;
                        var txt = _inputReaderStringBuilder.ToString();
                        if (!string.IsNullOrWhiteSpace(txt))
                        {
                            var index = GetIndexInWorkAreaConstraintedString(txt, _beginOfLineCurPos, p);
                            var slines = GetWorkAreaStringSplits(txt, _beginOfLineCurPos);

                            if (CursorTop == slines.Min(o => o.y))
                            {
                                SetCursorLeft(left);
                                Print(_prompt);
                            }
                            var enableConstraintConsolePrintInsideWorkArea = EnableConstraintConsolePrintInsideWorkArea;
                            EnableConstraintConsolePrintInsideWorkArea = false;
                            foreach (var (s, x, y, l) in slines)
                                if (y >= top && y <= bottom)
                                {
                                    SetCursorPos(x, y);
                                    ConsolePrint("".PadLeft(right - x, ' '));
                                    SetCursorPos(x, y);
                                    ConsolePrint(s);
                                }
                            EnableConstraintConsolePrintInsideWorkArea = enableConstraintConsolePrintInsideWorkArea;
                            SetCursorPos(p);
                        }
                    }
                }
            };
        }

        #region input processing

        static void ProcessInput(IAsyncResult asyncResult)
        {
            var s = (string)asyncResult.AsyncState;
            ProcessCommandLine(s, _evalCommandDelegate, true, true);
        }

        public static void ProcessCommandLine(
            string commandLine, 
            ExpressionEvaluationCommandDelegate evalCommandDelegate,
            bool outputStartNextLine = false,
            bool enableHistory = false)
        {
            
            if (commandLine != null)
            {
                if (outputStartNextLine) LineBreak();

                ExpressionEvaluationResult expressionEvaluationResult = null;

                try
                {
                    sc.CancelKeyPress += CancelKeyPress;
                    CommandLineProcessor.CancellationTokenSource = new CancellationTokenSource();
                    var task = Task.Run<ExpressionEvaluationResult>(
                        () => evalCommandDelegate(commandLine, _prompt==null?0:GetPrint(_prompt).Length),
                        CommandLineProcessor.CancellationTokenSource.Token
                        );

                    try
                    {
                        try
                        {
                            task.Wait(CommandLineProcessor.CancellationTokenSource.Token);
                        } catch (ThreadInterruptedException) {
                            // get interrupted after send input
                        }
                        expressionEvaluationResult = task.Result;
                    }
                    catch (OperationCanceledException)
                    {
                        var res = task.Result;
                        Errorln($"command canceled: {commandLine}");
                    }
                    finally
                    {
                        
                    }                    
                }
                catch (Exception ex)
                {
                    LogError(ex);                        
                }
                finally
                {
                    CommandLineProcessor.CancellationTokenSource.Dispose();
                    sc.CancelKeyPress -= CancelKeyPress;
                    lock (ConsoleLock)
                    {
                        /*if (!WorkArea.rect.IsEmpty && (WorkArea.rect.Y != CursorTop || WorkArea.rect.X != CursorLeft))
                            LineBreak();*/
                        RestoreDefaultColors();
                    }
                }
            }
            if (enableHistory && !string.IsNullOrWhiteSpace(commandLine))
                HistoryAppend(commandLine);
        }

        private static void CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
            CommandLineProcessor.CancellationTokenSource.Cancel();
        }

        public static int ReadCommandLine(
            string prompt="", 
            bool waitForReaderExited = true
            )
        {
            InputProcessor ??= CommandLineReader.ProcessInput;
            return BeginReadln(new AsyncCallback(InputProcessor), prompt, waitForReaderExited);
        }

        public static void SendInput(string text,bool sendEnter=true)
        {
            _sentInput = text + ((sendEnter)?Environment.NewLine:"");
            if (_inputReaderThread == null) return;
            StopBeginReadln();
            InputProcessor ??= CommandLineReader.ProcessInput;
            BeginReadln(new AsyncCallback(InputProcessor), _prompt, _waitForReaderExited);
        }

        public static int BeginReadln(
            AsyncCallback asyncCallback, 
            string prompt = "",
            bool waitForReaderExited = true)
        {
            _waitForReaderExited = waitForReaderExited;
            _prompt = prompt;            
            _inputReaderThread = new Thread(() =>
            {
                try
                {
                    var isRunning = true;
                    while (isRunning)
                    {
                        _inputReaderStringBuilder ??= new StringBuilder();
                        if (!_readingStarted)
                        {
                            lock (ConsoleLock)
                            {
                                Print(prompt);
                                _beginOfLineCurPos = CursorPos;
                            }
                            _readingStarted = true;
                        }
                        var eol = false;
                        while (!eol)
                        {
                            ConsoleKeyInfo c;
                            var printed = false;
                            string printedStr = "";
                            var (id, left, top, right, bottom) = ActualWorkArea;

                            if (sc.IsInputRedirected)
                            {
                                _sentInput = sc.In.ReadToEnd();
                                isRunning = false;
                            }

                            if (_sentInput == null)
                            { 
                                c = sc.ReadKey(true);
#if dbg
                                System.Diagnostics.Debug.WriteLine($"{c.KeyChar}={c.Key}");
#endif
#region handle special keys - edition mode, movement

                                (id, left, top, right, bottom) = ActualWorkArea;

                                switch (c.Key)
                                {
                                    case ConsoleKey.Enter:
                                        eol = true;
                                        break;
                                    case ConsoleKey.Escape:
                                        HideCur();
                                        CleanUpReadln();
                                        ShowCur();
                                        break;
                                    case ConsoleKey.Home:
                                        lock (ConsoleLock)
                                        {
                                            SetCursorPosConstraintedInWorkArea(_beginOfLineCurPos);
                                        }
                                        break;
                                    case ConsoleKey.End:
                                        lock (ConsoleLock)
                                        {
                                            var slines = GetWorkAreaStringSplits(_inputReaderStringBuilder.ToString(), _beginOfLineCurPos);
                                            var (txt, nx, ny, l) = slines.Last();
                                            SetCursorPosConstraintedInWorkArea(nx + l, ny);
                                        }
                                        break;
                                    case ConsoleKey.Tab:
                                        lock (ConsoleLock)
                                        {
                                            printedStr = "".PadLeft(TabLength, ' ');
                                            printed = true;
                                        }
                                        break;
                                    case ConsoleKey.LeftArrow:
                                        lock (ConsoleLock)
                                        {
                                            var p = CursorPos;
                                            if (p.Y == _beginOfLineCurPos.Y)
                                            {
                                                if (p.X > _beginOfLineCurPos.X)
                                                    SetCursorLeft(p.X - 1);
                                            }
                                            else
                                            {
                                                var x = p.X - 1;
                                                if (x < left)
                                                    SetCursorPosConstraintedInWorkArea(right - 1, p.Y - 1);
                                                else
                                                    SetCursorLeft(x);
                                            }
                                        }
                                        break;
                                    case ConsoleKey.RightArrow:
                                        lock (ConsoleLock)
                                        {
                                            var txt = _inputReaderStringBuilder.ToString();
                                            var index = GetIndexInWorkAreaConstraintedString(txt, _beginOfLineCurPos, CursorPos);
                                            if (index < txt.Length)
                                                SetCursorPosConstraintedInWorkArea(CursorLeft + 1, CursorTop);
                                        }
                                        break;
                                    case ConsoleKey.Backspace:
                                        lock (ConsoleLock)
                                        {
                                            var txt = _inputReaderStringBuilder.ToString();
                                            var index = GetIndexInWorkAreaConstraintedString(txt, _beginOfLineCurPos, CursorPos) - 1;
                                            var x = CursorLeft - 1;
                                            var y = CursorTop;
                                            if (index >= 0)
                                            {
                                                _inputReaderStringBuilder.Remove(index, 1);
                                                _inputReaderStringBuilder.Append(" ");
                                                HideCur();
                                                SetCursorPosConstraintedInWorkArea(ref x, ref y);
                                                var slines = GetWorkAreaStringSplits(_inputReaderStringBuilder.ToString(), _beginOfLineCurPos);
                                                var enableConstraintConsolePrintInsideWorkArea = EnableConstraintConsolePrintInsideWorkArea;
                                                EnableConstraintConsolePrintInsideWorkArea = false;
                                                foreach (var (line, lx, ly, l) in slines)
                                                    if (ly >= top && ly <= bottom)
                                                    {
                                                        SetCursorPos(lx, ly);
                                                        ConsolePrint("".PadLeft(right - lx, ' '));
                                                        SetCursorPos(lx, ly);
                                                        ConsolePrint(line);
                                                    }
                                                _inputReaderStringBuilder.Remove(_inputReaderStringBuilder.Length - 1, 1);
                                                EnableConstraintConsolePrintInsideWorkArea = enableConstraintConsolePrintInsideWorkArea;
                                                SetCursorPos(x, y);
                                                ShowCur();
                                            }
                                        }
                                        break;
                                    case ConsoleKey.Delete:
                                        lock (ConsoleLock)
                                        {
                                            var txt = _inputReaderStringBuilder.ToString();
                                            var index = GetIndexInWorkAreaConstraintedString(txt, _beginOfLineCurPos, CursorPos);
                                            var x = CursorLeft;
                                            var y = CursorTop;
                                            if (index >= 0 && index < txt.Length)
                                            {
                                                _inputReaderStringBuilder.Remove(index, 1);
                                                _inputReaderStringBuilder.Append(" ");
                                                HideCur();
                                                SetCursorPosConstraintedInWorkArea(ref x, ref y);
                                                var slines = GetWorkAreaStringSplits(_inputReaderStringBuilder.ToString(), _beginOfLineCurPos);
                                                var enableConstraintConsolePrintInsideWorkArea = EnableConstraintConsolePrintInsideWorkArea;
                                                EnableConstraintConsolePrintInsideWorkArea = false;
                                                foreach (var (line, lx, ly, l) in slines)
                                                    if (ly >= top && ly <= bottom)
                                                    {
                                                        SetCursorPos(lx, ly);
                                                        ConsolePrint("".PadLeft(right - lx, ' '));
                                                        SetCursorPos(lx, ly);
                                                        ConsolePrint(line);
                                                    }
                                                _inputReaderStringBuilder.Remove(_inputReaderStringBuilder.Length - 1, 1);
                                                EnableConstraintConsolePrintInsideWorkArea = enableConstraintConsolePrintInsideWorkArea;
                                                SetCursorPos(x, y);
                                                ShowCur();
                                            }
                                        }
                                        break;
                                    case ConsoleKey.UpArrow:
                                        lock (ConsoleLock)
                                        {
                                            if (CursorTop == _beginOfLineCurPos.Y)
                                            {
                                                var h = GetBackwardHistory();
                                                if (h != null)
                                                {
                                                    HideCur();
                                                    CleanUpReadln();
                                                    _inputReaderStringBuilder.Append(h);
                                                    ConsolePrint(h);
                                                    ShowCur();
                                                }
                                            }
                                            else
                                            {
                                                SetCursorPosConstraintedInWorkArea(
                                                    (CursorTop - 1) == _beginOfLineCurPos.Y ?
                                                        Math.Max(_beginOfLineCurPos.X, CursorLeft) : CursorLeft,
                                                    CursorTop - 1);
                                            }
                                        }
                                        break;
                                    case ConsoleKey.DownArrow:
                                        lock (ConsoleLock)
                                        {
                                            var slines = GetWorkAreaStringSplits(_inputReaderStringBuilder.ToString(), _beginOfLineCurPos);
                                            if (CursorTop == slines.Max(o => o.y))
                                            {
                                                var fh = GetForwardHistory();
                                                if (fh != null)
                                                {
                                                    HideCur();
                                                    CleanUpReadln();
                                                    _inputReaderStringBuilder.Append(fh);
                                                    ConsolePrint(fh);
                                                    ShowCur();
                                                }
                                            }
                                            else
                                            {
                                                var (txt, nx, ny, l) = slines.Where(o => o.y == CursorTop + 1).First();
                                                SetCursorPosConstraintedInWorkArea(Math.Min(CursorLeft, nx + l), CursorTop + 1);
                                            }
                                        }
                                        break;
                                    default:
                                        printedStr = c.KeyChar + "";
                                        printed = true;
                                        break;
                                }

#endregion
                            }
                            else
                            {
                                printedStr = _sentInput;
                                _sentInput = null;
                                printed = true;
                                eol = printedStr.EndsWith(Environment.NewLine);
                                if (eol) printedStr = printedStr.Trim();
                            }

                            if (printed)
                            {
                                var index = 0;
                                var insert = false;
                                lock (ConsoleLock)
                                {
                                    var x0 = CursorLeft;
                                    var y0 = CursorTop;
                                    var txt = _inputReaderStringBuilder.ToString();
                                    index = GetIndexInWorkAreaConstraintedString(txt, _beginOfLineCurPos, x0, y0);
                                    insert = index - txt.Length < 0;

                                    if (insert)
                                    {
                                        HideCur();
                                        var x = x0;
                                        var y = y0;
                                        SetCursorPosConstraintedInWorkArea(ref x, ref y);
                                        _inputReaderStringBuilder.Insert(index, printedStr);
                                        var slines = GetWorkAreaStringSplits(_inputReaderStringBuilder.ToString(), _beginOfLineCurPos);
                                        var enableConstraintConsolePrintInsideWorkArea = EnableConstraintConsolePrintInsideWorkArea;
                                        EnableConstraintConsolePrintInsideWorkArea = false;
                                        foreach (var (line, lx, ly, l) in slines)
                                            if (ly >= top && ly <= bottom)
                                            {                                                
                                                SetCursorPos(lx, ly);
                                                ConsolePrint(line);
                                            }
                                        EnableConstraintConsolePrintInsideWorkArea = enableConstraintConsolePrintInsideWorkArea;
                                        x+=printedStr.Length;
                                        SetCursorPosConstraintedInWorkArea(ref x, ref y);
                                        ShowCur();
                                    }
                                    if (!insert)
                                    {
                                        _inputReaderStringBuilder.Append(printedStr);
                                        ConsolePrint(printedStr, false);
                                    }
                                }
                            }

                            if (eol) break;
                        }

                        // process input
                        var s = _inputReaderStringBuilder.ToString();
                        _inputReaderStringBuilder.Clear();
                        asyncCallback?.Invoke(
                            new BeginReadlnAsyncResult(s)
                            );
                        _readingStarted = false;
                        if (_nextPrompt!=null)
                        {
                            prompt = _prompt = _nextPrompt;
                            _nextPrompt = null;
                        }
                    }
                }
                catch (ThreadInterruptedException) { }
                catch (Exception ex)
                {
                    LogException(ex,"input stream reader crashed");
                }
            })
            {
                Name = "input stream reader"
            };
            _inputReaderThread.Start();
            if (waitForReaderExited) _inputReaderThread.Join();
            return ReturnCodeOK;
        }

        public static void WaitReadln()
        {
            _inputReaderThread?.Join();
        }

        public static void CleanUpReadln()
        {
            if (_inputReaderThread != null)
            {
                lock (ConsoleLock)
                {
                    var (id,left, top, right, bottom) = ActualWorkArea;
                    SetCursorPosConstraintedInWorkArea(_beginOfLineCurPos);
                    var txt = _inputReaderStringBuilder.ToString();
                    var slines = GetWorkAreaStringSplits(txt, _beginOfLineCurPos);
                    var enableConstraintConsolePrintInsideWorkArea = EnableConstraintConsolePrintInsideWorkArea;
                    EnableConstraintConsolePrintInsideWorkArea = false;
                    foreach (var (line, x, y, l) in slines)
                        if (y>=top && y<= bottom)
                        {
                            SetCursorPos(x, y);
                            ConsolePrint("".PadLeft(right - x, ' '));
                        }
                    EnableConstraintConsolePrintInsideWorkArea = enableConstraintConsolePrintInsideWorkArea;
                    SetCursorPosConstraintedInWorkArea(_beginOfLineCurPos);
                    _inputReaderStringBuilder.Clear();
                }
            }
        }

        public static void StopBeginReadln()
        {
            _inputReaderThread?.Interrupt();
            _inputReaderThread = null;
            _readingStarted = false;
        }

#endregion

        #region history operations

        public static string GetBackwardHistory()
        {
            if (_historyIndex < 0)
                _historyIndex = _history.Count+1;
            if (_historyIndex >= 1)
                _historyIndex--;
            //System.Diagnostics.Debug.WriteLine($"{_historyIndex}");
            return (_historyIndex < 0 || _history.Count == 0 || _historyIndex >= _history.Count) ? null : _history[_historyIndex];
        }

        public static string GetForwardHistory()
        {
            if (_historyIndex < 0 || _historyIndex >= _history.Count)
                _historyIndex = _history.Count;
            if (_historyIndex < _history.Count - 1) _historyIndex++;

            //System.Diagnostics.Debug.WriteLine($"{_historyIndex}");
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
