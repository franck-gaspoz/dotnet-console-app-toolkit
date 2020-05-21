//#define dbg

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
            if (x == -1) x = Console.WindowLeft + Console.WindowWidth - 1;
            if (y == -1) y = Console.WindowTop + Console.WindowHeight - 1;
            if (w == -1) w = Console.WindowWidth;
            if (h == -1) h = Console.WindowHeight;
            return (x, y, w, h);
        }

        public static bool ValidCoords(int x,int y) => x>=0 && y>=0 && x < Console.BufferWidth && y < Console.BufferHeight;

        public static void FixCoords(ref int x,ref int y)
        {
            x = Math.Max(0, Math.Min(x, Console.BufferWidth - 1));
            y = Math.Max(0, Math.Min(y, Console.BufferHeight - 1));
        }

        public static int FixX(int x) => Math.Max(0, Math.Min(x, Console.BufferWidth - 1));

        public static int FixY(int y) => y = Math.Max(0, Math.Min(y, Console.BufferHeight - 1));

        public static void DrawRect(ConsoleColor backgroundColor, int rx = 0, int ry = -1, int rw = -1, int rh = -1)
        {
            var p = CursorPos;
            var (x, y, w, h) = GetCoords(rx, ry, rw, rh);
            if (!ValidCoords(x, y))
            {
#if dbg
                OutputTo("./trace.txt");
                Println($"skip draw rect (unvalid coords)");
                OutputTo();
#endif
                return;
            }

#if dbg
            OutputTo("./trace.txt");
            Infos();
            Println($"x={x} y={y} w={w} h={h}");
            OutputTo();
#endif

            var s = "".PadLeft(w, ' ');
            for (int i = 0; i < h; i++)
                Print($"{Crx(x)}{Cry(y + i)}{B(backgroundColor)}{s}");
            SetCursorPos(p);
        }

        public abstract void Draw(bool drawBackground = true);
        public abstract void UpdateDraw(bool erase = false,bool forceDraw=false);
        public abstract void Erase();
    }
}
