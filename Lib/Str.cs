using System;
using System.Collections.Generic;
using System.Linq;

namespace DotNetConsoleSdk.Lib
{
    public static class Str
    {
        #region data to text operations

        public static string DumpNullStringAsText = "{null}";

        public static string DumpAsText(object o)
        {
            if (o == null) return DumpNullStringAsText ?? null;
            if (o is string s) return $"\"{s}\"";
            return o.ToString();
        }

        public static string Dump(object[] t)
        {
            return string.Join(',', t.Select(x => DumpAsText(x)));
        }

        public static string HumanFormatOfSize(long bytes, int digits = 1, string sep = " ", string bigPostFix = "")
        {
            long absB = bytes == long.MinValue ? long.MaxValue : Math.Abs(bytes);
            if (absB < 1024)
            {
                return bytes + sep + "B";
            }
            long value = absB;
            Stack<long> values = new Stack<long>();
            char ci = '?';
            var t = new char[] { 'K', 'M', 'G', 'T', 'P', 'E' };
            int n = 0;
            while (n < t.Length && value > 0)
            {
                for (int i = 40; i >= 0 && absB > 0xfffccccccccccccL >> i; i -= 10)
                {
                    value >>= 10;
                    if (value > 0)
                    {
                        ci = t[n++];
                        values.Push(value);
                    }
                }
            }
            value = values.Pop();
            if (values.Count > 0) value = values.Pop();
            value *= Math.Sign(bytes);
            return String.Format("{0:F" + digits + "}" + sep + "{1}" + bigPostFix + "B", value / 1024d, ci);
        }

        #endregion
    }
}
