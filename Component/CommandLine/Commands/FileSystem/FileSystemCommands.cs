using DotNetConsoleSdk.Component.CommandLine.CommandModel;
using System.Collections.Generic;
using System.IO;

namespace DotNetConsoleSdk.Component.CommandLine.Commands.FileSystem
{
    [Commands]
    public class FileSystemCommands
    {
        [Command("test find command")]
        public List<string> Find(
            [Parameter("search path")] DirectoryPath path,
            [Option("file", "searched filename pattern", true, true)] string filename,
            [Option("contains", "files that contains the string", true, true)] string contains
            )
        {

            return null;
        }
    }
}
