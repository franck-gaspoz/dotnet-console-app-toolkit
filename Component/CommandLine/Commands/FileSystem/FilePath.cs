using DotNetConsoleSdk.Component.CommandLine.CommandModel;
using System.IO;

namespace DotNetConsoleSdk.Component.CommandLine.Commands.FileSystem
{
    [CustomParamaterType]
    public class FilePath
    {
        public readonly FileInfo FileInfo;

        public FilePath(string path)
        {
            FileInfo = new FileInfo(path);
        }

        public override string ToString()
        {
            return FileInfo.FullName;
        }
    }
}
