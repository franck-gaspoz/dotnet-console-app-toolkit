using DotNetConsoleSdk.Component.CommandLine.CommandModel;
using static DotNetConsoleSdk.DotNetConsole;
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

        public bool CheckExists(bool dumpError = true)
        {
            if (!FileInfo.Exists)
            {
                if (dumpError)
                    Errorln($"file doesn't exists: {this}");
                return false;
            }
            return true;
        }

        public override string ToString()
        {
            return FileInfo.FullName;
        }
    }
}
