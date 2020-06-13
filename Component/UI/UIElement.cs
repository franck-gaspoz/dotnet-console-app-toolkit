//#define dbg

using sc = System.Console;
using System;
using static DotNetConsoleAppToolkit.DotNetConsole;

namespace DotNetConsoleAppToolkit.Component.UI
{
    public abstract class UIElement
    {
        /// <summary>
        /// this setting limit wide of lines (available width -1) to prevent sys console to automatically put a line break when reaching end of line (console bug ?)
        /// </summary>
        public static bool AvoidConsoleAutoLineBreakAtEndOfLine = false;

        public static bool RedrawUIElementsEnabled = true;

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
            // (1) dos console (eg. vs debug consolehep) set WindowTop as y scroll position. WSL console doesn't (still 0)
            // scroll -> native dos console set WindowTop and WindowLeft as base scroll coordinates
            // if WorkArea defined, we must use absolute coordinates and not related
            // CursorLeft and CursorTop are always good
            lock (ConsoleLock)
            {
                if (x < 0) x = sc.WindowLeft + sc.WindowWidth + x;
                
                if (y < 0) y = /*sc.WindowTop (fix 1) */ + sc.WindowHeight + y;

                if (w < 0) w = sc.WindowWidth + ((AvoidConsoleAutoLineBreakAtEndOfLine) ? -1 : 0) + (w + 1)
                        /*+ sc.WindowLeft*/;

                if (h < 0) h = sc.WindowHeight + h
                        + sc.WindowTop; /* fix 1*/
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
                var s = "".PadLeft(w, ' ');
                for (int i = 0; i < h; i++)
                    Print($"{Crx(x)}{Cry(y + i)}{B(backgroundColor)}{s}");
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
                var s = "".PadLeft(w, ' ');
                for (int i = 0; i < h; i++)
                    Print($"{Crx(x)}{Cry(y + i)}{B(backgroundColor)}{s}");
            }
        }

        public abstract void Draw(bool viewSizeChanged = true);
        public abstract void UpdateDraw(bool viewSizeChanged = false);
        public abstract void Erase();
    }
}
