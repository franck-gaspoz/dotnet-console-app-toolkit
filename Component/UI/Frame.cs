//#define dbg

using System;
using System.Collections.Generic;
using sc = System.Console;
using System.Threading;
using static DotNetConsoleSdk.DotNetConsole;

namespace DotNetConsoleSdk.Component.UI
{
    public delegate List<string> GetContentDelegate(Frame x);

    public class Frame : UIElement
    {
        public readonly int Id;
        public GetContentDelegate GetContent;
        public ConsoleColor BackgroundColor;

        public int X { get; protected set; }
        public int Y { get; protected set; }
        public int W { get; protected set; }
        public int H { get; protected set; }

        public int ActualX { get; protected set; } = -1;
        public int ActualY { get; protected set; } = -1;
        public int ActualWidth { get; protected set; } = -1;
        public int ActualHeight { get; protected set; } = -1;

        public int FixedX { get; protected set; } = -1;
        public int FixedY { get; protected set; } = -1;
        public int FixedWidth { get; protected set; } = -1;
        public int FixedHeight { get; protected set; } = -1;

        public bool AlwaysPaintBackground = false;
        public int UpdateTimerInterval = 500;
        public Thread _updateThread;

        protected bool Painted = false;

        public Frame(
            GetContentDelegate getContent, 
            ConsoleColor backgroundColor, 
            int x = 0, 
            int y = -1, 
            int w = -1,
            int h = -1, 
            DrawStrategy drawStrategy = DrawStrategy.OnViewResizedOnly, 
            bool mustRedrawBackground = false,
            int updateTimerInterval = 0)
            : base(drawStrategy)
        {
            Id = _uiid++;
            GetContent = getContent;
            BackgroundColor = backgroundColor;
            X = x;
            Y = y;
            W = w;
            H = h;
            AlwaysPaintBackground = mustRedrawBackground;
            UpdateTimerInterval = updateTimerInterval;
            if (drawStrategy==DrawStrategy.OnTime)
            {
                _updateThread = new Thread(UpdateThreadBody)
                { 
                    Name = $"update thread #{Id}" 
                };
                _updateThread.Start();
            }
        }

        void UpdateThreadBody()
        {
            try
            {
                while (true)
                {
                    Thread.Sleep(UpdateTimerInterval);
                    lock (ConsoleLock)
                    {
                        BackupCursorPos();
                        Draw(AlwaysPaintBackground);
                        RestoreCursorPos();
                    }
                }
            }
            catch (ThreadInterruptedException) { }
            catch (Exception ex)
            {
                LogError($"update thread #{Id} crashed: " + ex.Message);
            }
        }

        (int x,int y,int w,int h) GetCurrentCoords()
        {
            var (nx, ny, nw, nh) = GetCoords(X, Y, W, H);
            if (DotNetConsole.ViewResizeStrategy == ViewResizeStrategy.FitViewSize)
                return (nx, ny, nw, nh);
            if (!Painted)
            {
                FixedX = nx;
                FixedY = ny;
                FixedWidth = nw;
                FixedHeight = nh;
                Painted = true;
            }
            return (FixedX, FixedY, FixedWidth, FixedHeight);
        }

        public override void Draw(bool viewSizeChanged = true)
        {
            lock (ConsoleLock)
            {
                var redrawUIElementsEnabled = RedrawUIElementsEnabled;
                RedrawUIElementsEnabled = false;
                var enableConstraintConsolePrintInsideWorkArea = EnableConstraintConsolePrintInsideWorkArea;
                EnableConstraintConsolePrintInsideWorkArea = false;
                var p = CursorPos;
                var (x, y, w, h) = GetCurrentCoords();
                BackupCoords(x, y, w, h);
                var content = GetContent?.Invoke(this);
                //HideCur();
                if (viewSizeChanged || AlwaysPaintBackground)
                    DrawRectAt(BackgroundColor, x, y, w, h);

                for (int i = 0; i < content.Count; i++)
                {
                    if (i < h)
                    {
                        SetCursorPos(x, y + i);
                        CropX = x + w - 1;
                        Print(content[i]);
                        CropX = -1;
                    }
                }

                SetCursorPos(p);
                //ShowCur();
                EnableConstraintConsolePrintInsideWorkArea = enableConstraintConsolePrintInsideWorkArea;
                RedrawUIElementsEnabled = redrawUIElementsEnabled;
            }
        }

        void BackupCoords(int x, int y, int w, int h)
        {
            ActualX = x;
            ActualY = y;
            ActualWidth = w;
            ActualHeight = h;
        }

        public override void Erase()
        {
            lock (ConsoleLock)
            {
                var p = CursorPos;                
                var redrawUIElementsEnabled = RedrawUIElementsEnabled;
                var enableConstraintConsolePrintInsideWorkArea = EnableConstraintConsolePrintInsideWorkArea;
                EnableConstraintConsolePrintInsideWorkArea = false;
                RedrawUIElementsEnabled = false;
                HideCur();
                DrawRect(Console.BackgroundColor, ActualX, ActualY, ActualWidth, ActualHeight);
                SetCursorPos(p);
                ShowCur();
                EnableConstraintConsolePrintInsideWorkArea = enableConstraintConsolePrintInsideWorkArea;
                RedrawUIElementsEnabled = redrawUIElementsEnabled;
            }
        }

        public override void UpdateDraw( bool viewSizeChanged = false)
        {
            lock (ConsoleLock)
            {
                if (viewSizeChanged && !ClearOnViewResized && DotNetConsole.ViewResizeStrategy == ViewResizeStrategy.FitViewSize)
                    Erase();
                List<DrawStrategy> ignorableStrategies = new List<DrawStrategy>()
                    { DrawStrategy.OnPrint , DrawStrategy.OnTime };
                if (!viewSizeChanged && !ignorableStrategies.Contains(DrawStrategy)) return;
                Draw(viewSizeChanged);
            }
        }
    }
}
