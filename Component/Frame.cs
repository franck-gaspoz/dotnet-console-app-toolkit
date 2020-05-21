//#define dbg

using System;
using System.Collections.Generic;
using System.Threading;
using static DotNetConsoleSdk.DotNetConsoleSdk;

namespace DotNetConsoleSdk.Component
{
    public delegate List<string> GetContentDelegate(Frame x);

    public class Frame : UIElement
    {
        public readonly int Id;
        public GetContentDelegate GetContent;
        public ConsoleColor BackgroundColor;
        public int X = 0;
        public int Y = -1;
        public int W = -1;
        public int H = -1;
        public int ActualX = 0;
        public int ActualY = -1;
        public int ActualWidth = -1;
        public int ActualHeight = -1;
        public bool MustRedrawBackground = false;
        public int UpdateTimerInterval = 500;
        public Thread _updateThread;

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
            MustRedrawBackground = mustRedrawBackground;
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
                        Draw(MustRedrawBackground);
                        RestoreCursorPos();
                    }
                }
            }
            catch { }
        }

        public override void Draw(bool drawBackground = true)
        {
            lock (ConsoleLock)
            {
                var redrawUIElementsEnabled = RedrawUIElementsEnabled;
                RedrawUIElementsEnabled = false;
                var p = CursorPos;
                var (x, y, w, h) = GetCoords(X, Y, W, H);
                BackupCoords(x, y, w, h);
                var content = GetContent?.Invoke(this);
                HideCur();
                if (drawBackground)
                    DrawRectAt(BackgroundColor, x, y, w, h);

                for (int i = 0; i < content.Count; i++)
                {
                    SetCursorPos(x, y + i);
                    Print(content[i]);
                }

                SetCursorPos(p);
                ShowCur();
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
                var redrawUIElementsEnabled = RedrawUIElementsEnabled;
                RedrawUIElementsEnabled = false;
                DrawRect(Console.BackgroundColor, ActualX, ActualY, ActualWidth, ActualHeight);
                RedrawUIElementsEnabled = redrawUIElementsEnabled;
            }
        }

        public override void UpdateDraw(
            bool erase = false,
            bool forceDraw = false)
        {
            lock (ConsoleLock)
            {
                if (erase) Erase();
                List<DrawStrategy> ignorableStrategies = new List<DrawStrategy>()
            { DrawStrategy.OnPrint , DrawStrategy.OnTime };
                if (!forceDraw && !ignorableStrategies.Contains(DrawStrategy)) return;
                Draw(/*forceDraw ||*/ MustRedrawBackground);
            }
        }
    }
}
