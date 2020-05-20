using System;
using static DotNetConsoleSdk.DotNetConsoleSdk;

namespace DotNetConsoleSdk
{
    class Program
    {
        static void Main(string[] args)
        {
            Clear();

            AddBar((bar) =>
            {
                var s = "".PadLeft(bar.BW,'-');
                var t = "  Console tool: commands tester  ";                
                var r = $"{Bdarkblue}{Cyan}{s}{Br}";
                r += $"{Bdarkblue}{Cyan}|{t}{White}{"".PadLeft(bar.BW-2-t.Length)}{Cyan}|{Br}";
                r += $"{Bdarkblue}{Cyan}{s}{Br}";
                return r;
            }, ConsoleColor.DarkBlue,0,0,-1,3,true,false);

            SetCursorPos(0, 4);
            Infos();
            LineBreak();

            AddBar((bar) =>
            {
                var r = $"{Bdarkblue}{Green}Cursor pos: {White}X={Cyan}{CursorLeft}{Green},{White}Y={Cyan}{CursorTop}{White}";
                r += $" | {Green}bar pos: {White}X={Cyan}{bar.BX}{Green},{White}Y={Cyan}{bar.BY}{White}";
                return r;
            }, ConsoleColor.DarkBlue);

            CommandTester("> ");
        }
    }
}
