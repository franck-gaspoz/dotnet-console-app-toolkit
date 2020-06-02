using DotNetConsoleSdk.Component.CommandLine.CommandModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace DotNetConsoleSdk.Component.CommandLine.Commands
{
    public class TestCommands
    {
        [Command("test find command")]
        public List<string> Find(
            [Parameter("search path")] string path,
            [Option("file", "searched filename pattern", true, true)] string filename,
            [Option("contains", "files that contains the string", true, true)] string contains
            )
        {
            return null;
        }
    }
}
