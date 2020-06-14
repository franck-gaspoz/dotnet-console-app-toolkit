using System.IO;
using System.Runtime.InteropServices;

namespace DotNetConsoleAppToolkit.Lib
{
    public static class FIleReader
    {
        public static (string[] lines,OSPlatform platform) ReadAllLines(string path)
        {
            string[] r = null;
            var txt = File.ReadAllText(path);
            var sep_linux = "\n";
            var sep_osx = "\r";
            var sep_windows = "\r\n";
            var doublecrlf = "\r\r\n";

            static int Count(string searched,string txt)
            {
                var i = 0;
                var cnt=0;
                while (i < txt.Length && i > -1)
                    if ((i = txt.IndexOf(searched, i)) > -1)
                    {
                        cnt++;
                        i++;
                    }
                return cnt;
            }            
            var ospl = OSPlatform.Windows;
            
            var cnt_linux = Count(sep_linux, txt);
            var cnt_windows = Count(sep_windows, txt);
            var cnt_osx = Count(sep_osx, txt);
            var cnt_doublecrlf = Count(doublecrlf, txt);

            if (cnt_doublecrlf>0)
            {
                txt = txt.Replace(doublecrlf, sep_windows);
                cnt_linux = Count(sep_linux, txt);
                cnt_windows = Count(sep_windows, txt);
                cnt_osx = Count(sep_osx, txt);
            }
            if (cnt_windows >= cnt_linux && cnt_windows >= cnt_osx)
            {
                ospl = OSPlatform.Windows;
                r = txt.Split(sep_windows);
            }
            else
            {
                if (cnt_linux >= cnt_windows && cnt_linux >= cnt_osx)
                {
                    ospl = OSPlatform.Linux;
                    r = txt.Split(sep_linux);
                }
                else if (cnt_osx >= cnt_windows && cnt_osx >= cnt_linux)
                {
                    ospl = OSPlatform.OSX;
                    r = txt.Split(sep_osx);
                }
            }
            return (r,ospl);
        }
    }
}
