using System;
using System.Collections.Generic;
using System.Text;

namespace DotNetConsoleAppToolkit.Console
{
    public static class ANSI
    {
        public static readonly string ESC = ((char)27) + "";
        public static readonly string CSI = $"{ESC}[";
        public static readonly string CRLF = (char)13 + ((char)10 + "");
        public static readonly string LNBRK = $"{ESC}[0m{CRLF}";

        public static string Set3BitsColors(int foregroundNum,int backgroundNum) =>
            $"{CSI}0m{CSI}{(((backgroundNum & 0b1000) != 0) ? "4" : "10")}{backgroundNum & 0b111}m{CSI}{(((foregroundNum & 0b1000) != 0)?"3":"9")}{foregroundNum & 0b111}m";
    }
}
