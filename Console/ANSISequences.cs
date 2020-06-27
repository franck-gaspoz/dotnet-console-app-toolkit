using System;
using System.Collections.Generic;
using System.Text;

namespace DotNetConsoleAppToolkit.Console
{
    public static class ANSISequences
    {
        public static readonly string ESC = ((char)27) + "";
        public static readonly string CRLF = (char)13 + ((char)10 + "");
        public static readonly string LNBRK = $"{ESC}[0m{CRLF}";
    }
}
