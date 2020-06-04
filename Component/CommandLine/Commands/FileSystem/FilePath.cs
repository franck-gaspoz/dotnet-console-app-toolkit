using System.IO;
using static DotNetConsoleSdk.DotNetConsole;

namespace DotNetConsoleSdk.Component.CommandLine.Commands.FileSystem
{
    public class FilePath : FileSystemPath
    {
        public readonly FileInfo FileInfo;

        public FilePath(string path) : base(new FileInfo(path))
        {
            FileInfo = (FileInfo)FileSystemInfo;
        }

        public override bool CheckExists(bool dumpError = true)
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
