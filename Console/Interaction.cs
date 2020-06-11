using DotNetConsoleSdk.Component.CommandLine.CommandLineReader;
using System;
using static DotNetConsoleSdk.DotNetConsole;

namespace DotNetConsoleSdk.Console
{
    public static class Interaction
    {
        /// <summary>
        /// ask a question, read input to enter. returns true if input is 'y' or 'Y'
        /// </summary>
        /// <param name="question"></param>
        /// <returns>returns true if input is 'y' or 'Y'</returns>
        public static bool Confirm(string question)
        {
            var r = false;
            void endReadln(IAsyncResult result)
            {
                r = result.AsyncState?.ToString()?.ToLower() == "y";
            }
            var cmdlr = new CommandLineReader(question + "? ", null);
            cmdlr.BeginReadln(endReadln, null, true, false);
            Println();
            return r;
        }


    }
}
