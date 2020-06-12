using DotNetConsoleSdk.Component.CommandLine.CommandModel;
using System;
using System.Collections.Generic;
using System.Text;
using sc = System.Console;
using static DotNetConsoleSdk.DotNetConsole;

namespace DotNetConsoleSdk.Component.CommandLine.Commands
{
    [Commands("tests commands")]
    public class TestCommands
    {
        [Command("print cursor info")]
        public void CursorInfo() => Println($"crx={sc.CursorLeft} cry={sc.CursorTop}");
    }
}
