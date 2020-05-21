//#define dbg

using sc = System.Console;
using System;
using static DotNetConsoleSdk.DotNetConsoleSdk;

namespace DotNetConsoleSdk.Component
{
    public abstract class UIElement
    {
        public static bool _redrawUIElementsEnabled = true;

        protected static int _uiid = 0;

        public readonly DrawStrategy DrawStrategy;

        public UIElement(
            DrawStrategy drawStrategy
            )
        {
            DrawStrategy = drawStrategy;
        }

        public static (int x, int y, int w, int h) GetCoords(int x, int y, int w, int h)
        {
            lock (ConsoleLock)
            {
                if (x == -1) x = sc.WindowLeft + sc.WindowWidth - 1;
                if (y == -1) y = sc.WindowTop + sc.WindowHeight - 1;
                if (w == -1) w = sc.WindowWidth;
                if (h == -1) h = sc.WindowHeight;
                return (x, y, w, h);
            }
        }

        public static bool ValidCoords(int x, int y)
        {
            lock (ConsoleLock)
            {
                return x >= 0 && y >= 0 && x < sc.BufferWidth && y < sc.BufferHeight;
            }
        }

        public static void FixCoords(ref int x,ref int y)
        {
            lock (ConsoleLock)
            {
                x = Math.Max(0, Math.Min(x, sc.BufferWidth - 1));
                y = Math.Max(0, Math.Min(y, sc.BufferHeight - 1));
            }
        }

        public static int FixX(int x)
        {
            lock (ConsoleLock)
            {
                return Math.Max(0, Math.Min(x, sc.BufferWidth - 1));
            }
        }

        public static int FixY(int y)
        {
            lock (ConsoleLock)
            {
                return y = Math.Max(0, Math.Min(y, sc.BufferHeight - 1));
            }
        }

        public static void DrawRectAt(ConsoleColor backgroundColor,
            int x, int y , int w , int h)
        {
            lock (ConsoleLock)
            {
                if (!ValidCoords(x, y)) return;
                var p = CursorPos;
                var s = "".PadLeft(w, ' ');
                for (int i = 0; i < h; i++)
                    Print($"{Crx(x)}{Cry(y + i)}{B(backgroundColor)}{s}");
                SetCursorPos(p);
            }
        }

        public static void DrawRect(
            ConsoleColor backgroundColor, 
            int rx = 0, int ry = -1, int rw = -1, int rh = -1)
        {
            lock (ConsoleLock)
            {
                var (x, y, w, h) = GetCoords(rx, ry, rw, rh);
                if (!ValidCoords(x, y)) return;
                var p = CursorPos;
                var s = "".PadLeft(w, ' ');
                for (int i = 0; i < h; i++)
                    Print($"{Crx(x)}{Cry(y + i)}{B(backgroundColor)}{s}");
                SetCursorPos(p);
            }
        }

        public abstract void Draw(bool drawBackground = true);
        public abstract void UpdateDraw(bool erase = false,bool forceDraw=false);
        public abstract void Erase();
    }
}
