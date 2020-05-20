//#define dbg

using System;
using static DotNetConsoleSdk.DotNetConsoleSdk;

namespace DotNetConsoleSdk.Component
{
    public class Frame : UIElement
    {
        public readonly int Id;
        public Func<Frame, string> Content;
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

        public Frame(Func<Frame, string> content,
                     ConsoleColor backgroundColor,
                     int x = 0,
                     int y = -1,
                     int w = -1,
                     int h = -1,
                     DrawStrategy drawStrategy = DrawStrategy.OnViewResized,
                     bool mustRedrawBackground = false)
            : base(drawStrategy)
        {
            Id = _uiid++;
            Content = content;
            BackgroundColor = backgroundColor;
            X = x;
            Y = y;
            W = w;
            H = h;
            MustRedrawBackground = mustRedrawBackground;
        }

        public override void Draw(bool drawBackground = true)
        {
            var redrawUIElementsEnabled = _redrawUIElementsEnabled;
            _redrawUIElementsEnabled = false;
            var p = CursorPos;
            var (x, y, w, h) = GetCoords(X, Y, W, H);
            BackupCoords(x, y, w, h);
            var content = Content?.Invoke(this);
            HideCur();
            if (drawBackground)
                DrawRect(BackgroundColor, X, Y, W, H);

            SetCursorPos(x, y);
            Print(content);
            SetCursorPos(p);
            ShowCur();
            _redrawUIElementsEnabled = redrawUIElementsEnabled;
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
            var redrawUIElementsEnabled = _redrawUIElementsEnabled;
            _redrawUIElementsEnabled = false;
#if dbg
            OutputTo("./trace.txt");
            Println($"erase");
            OutputTo();
#endif
            DrawRect(Console.BackgroundColor, ActualX, ActualY, ActualWidth, ActualHeight);
            _redrawUIElementsEnabled = redrawUIElementsEnabled;
        }

        public override void UpdateDraw(
            bool erase = false,
            bool forceDraw = false)
        {
            if (erase) Erase();
            if (!forceDraw && DrawStrategy != DrawStrategy.Always) return;
#if dbg
            OutputTo("./trace.txt");
            Println($"update draw (force draw={forceDraw} | must redraw background={MustRedrawBackground})");
            OutputTo();
#endif
            Draw(forceDraw || MustRedrawBackground);
        }
    }
}
