//#define dbg

using System;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static DotNetConsoleAppToolkit.Component.CommandLine.CommandLineProcessor;
using static DotNetConsoleAppToolkit.DotNetConsole;
using sc = System.Console;

namespace DotNetConsoleAppToolkit.Component.CommandLine.CommandLineReader
{
    public class CommandLineReader
    {
        #region attributes

        public delegate ExpressionEvaluationResult ExpressionEvaluationCommandDelegate(string com,int outputX);

        Thread _inputReaderThread;
        
        string _prompt;
        StringBuilder _inputReaderStringBuilder;
        Point _beginOfLineCurPos;
        ExpressionEvaluationCommandDelegate _evalCommandDelegate;
        string _sentInput = null;
        bool _waitForReaderExited;
        bool _readingStarted;
        string _nextPrompt = null;
        readonly string _defaultPrompt = null;
        readonly CommandLineProcessor CommandLineProcessor;
        bool _ignoreNextKey = false;
        
        public Action<IAsyncResult> InputProcessor { get; set; }

        #endregion

        #region initialization operations

        public CommandLineReader(
            CommandLineProcessor commandLineProcessor = null,
            string prompt = null,
            ExpressionEvaluationCommandDelegate evalCommandDelegate = null)
        {
            CommandLineProcessor = commandLineProcessor;
            if (CommandLineProcessor!=null && CommandLineProcessor!=null) CommandLineProcessor.CmdLineReader = this;
            _defaultPrompt = prompt ?? $"{Green}> {White}";
            Initialize(evalCommandDelegate);
        }

        public void SetPrompt(string prompt=null)
        {
            _nextPrompt = prompt ?? _defaultPrompt;
        }

        void Initialize(ExpressionEvaluationCommandDelegate evalCommandDelegate = null)
        {
            if (evalCommandDelegate==null && CommandLineProcessor!=null) _evalCommandDelegate = CommandLineProcessor.Eval;
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
                        var (id,left, top, width, height) = ActualWorkArea();
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
                                if (y >= top && y <= height)
                                {
                                    SetCursorPos(x, y);
                                    ConsolePrint("".PadLeft(width - x, ' '));
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

        #endregion

        #region input processing

        void ProcessInput(IAsyncResult asyncResult)
        {
            var s = (string)asyncResult.AsyncState;
            ProcessCommandLine(s, _evalCommandDelegate, true, true);
        }

        public void ProcessCommandLine(
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
                        () => evalCommandDelegate(commandLine, _prompt == null ? 0 : GetPrint(_prompt).Length),
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
                    CommandLineProcessor.CancellationTokenSource = null;
                    sc.CancelKeyPress -= CancelKeyPress;
                    lock (ConsoleLock)
                    {
                        /*if (!WorkArea.rect.IsEmpty && (WorkArea.rect.Y != CursorTop || WorkArea.rect.X != CursorLeft))
                            LineBreak();*/      // case of auto line break (spacing)
                        RestoreDefaultColors();
                    }
                }
            }
            if (enableHistory && !string.IsNullOrWhiteSpace(commandLine))
                CommandLineProcessor.CmdsHistory.HistoryAppend(commandLine);
        }

        private void CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
            CommandLineProcessor.CancellationTokenSource?.Cancel();
        }

        public int ReadCommandLine(
            string prompt=null, 
            bool waitForReaderExited = true
            )
        {
            prompt ??= _defaultPrompt;
            InputProcessor ??= ProcessInput;
            return BeginReadln(new AsyncCallback(InputProcessor), prompt, waitForReaderExited);
        }

        public void SendInput(string text,bool sendEnter=true)
        {
            _sentInput = text + ((sendEnter)?Environment.NewLine:"");
            if (_inputReaderThread == null) return;
            StopBeginReadln();
            InputProcessor ??= ProcessInput;
            BeginReadln(new AsyncCallback(InputProcessor), _prompt, _waitForReaderExited);
        }

        public void IgnoreNextKey() { _ignoreNextKey = true; }

        public void SendNextInput(string text, bool sendEnter = true)
        {
            _sentInput = text + ((sendEnter) ? Environment.NewLine : "");
            _readingStarted = false;
        }

        public int BeginReadln(
            AsyncCallback asyncCallback, 
            string prompt = null,
            bool waitForReaderExited = true,
            bool loop=true
            )
        {            
            _waitForReaderExited = waitForReaderExited;
            prompt ??= _defaultPrompt;
            _prompt = prompt;
            bool noWorkArea = !InWorkArea;
            _inputReaderThread = new Thread(() =>
            {
                try
                {
                    var isRunning = true;
                    while (isRunning)
                    {
                        if (!loop)
                            isRunning = false;
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
                            var (id, left, top, right, bottom) = ActualWorkArea();

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

                                if (!_ignoreNextKey)
                                {
                                    (id, left, top, right, bottom) = ActualWorkArea();

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
                                                    var h = CommandLineProcessor.CmdsHistory.GetBackwardHistory();
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
                                                    var fh = CommandLineProcessor.CmdsHistory.GetForwardHistory();
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
                                }
                                else 
                                    _ignoreNextKey = false;

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
                                        x += printedStr.Length;
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

                        if (noWorkArea)
                            EnableConstraintConsolePrintInsideWorkArea = false;

                        asyncCallback?.Invoke(
                            new BeginReadlnAsyncResult(s)
                            );

                        if (noWorkArea)
                            EnableConstraintConsolePrintInsideWorkArea = true;

                        _readingStarted = false;
                        if (_nextPrompt!=null)
                        {
                            prompt = _prompt = _nextPrompt;
                            _nextPrompt = null;
                        }
                    }
                }
                catch (ThreadInterruptedException) { 
                }
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

        public void WaitReadln()
        {
            _inputReaderThread?.Join();
        }

        public void CleanUpReadln()
        {
            if (_inputReaderThread != null)
            {
                lock (ConsoleLock)
                {
                    var (id,left, top, right, bottom) = ActualWorkArea();
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

        public void StopBeginReadln()
        {
            _inputReaderThread?.Interrupt();
            _inputReaderThread = null;
            _readingStarted = false;
        }

        #endregion        
    }
}
