using System;
using System.Collections.Generic;
using System.Text;

namespace DotNetConsoleSdk.Component.CLI
{
    public static class CLI
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

        public static void InitializeCLI(string[] args)
        {
            SetArgs(args);
        }

        #endregion

        #region commands operations

        /// <summary>
        /// 1. parse command line
        /// 2. execute command        
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public static int Eval(string command)
        {
            return ReturnCodeOK;
        }

        #endregion
    }
}
