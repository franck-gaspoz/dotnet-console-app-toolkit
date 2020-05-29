using System;
using System.Collections.Generic;
using System.Text;

namespace DotNetConsoleSdk.Component.CLI
{
    public static class CommandEngine
    {
        public static int ReturnCodeOK = 0;
        public static int ReturnCodeError = 1;

        static string[] _args;

        #region cli methods

        public static string Arg(int n)
        {
            if (_args == null) return null;
            if (_args.Length <= n) return null;
            return _args[n];
        }

        public static bool HasArgs => _args != null && _args.Length > 0;

        static void SetArgs(string[] args)
        {
            _args = (string[])args?.Clone();
        }

        public static void InitializeCommandEngine(string[] args)
        {
            SetArgs(args);
        }

        #endregion

        #region commands operations

        /// <summary>
        /// 1. parse command line
        /// 2. execute command        
        /// </summary>
        /// <param name="expr">expression to be evaluated</param>
        /// <returns>return code</returns>
        public static int Eval(string expr)
        {
            var splits = SplitExpr(expr);
            
            return ReturnCodeOK;
        }

        public static string[] SplitExpr(string expr)
        {
            if (expr == null) return new string[] { };
            var splits = new List<string>();
            var t = expr.Trim().ToCharArray();
            var inQuotedStr = false;
            int i = 0;
            var curStr = "";
            while (i < t.Length )
            {
                var c = t[i];
                if (!inQuotedStr)
                {
                    if (c == ' ')
                    {
                        splits.Add(curStr);
                        curStr = "";
                    } else
                    {
                        if (c == '"')
                            inQuotedStr = true;
                        else
                            curStr += c;
                    }
                } else
                {
                    if (c == '"')
                        inQuotedStr = false;
                    else
                        curStr += c;
                }
                i++;
            }
            if (!string.IsNullOrWhiteSpace(curStr))
                splits.Add(curStr);
            return splits.ToArray();
        }

        #endregion
    }
}
