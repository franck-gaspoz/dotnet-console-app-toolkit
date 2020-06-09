using System.IO;
using static DotNetConsoleSdk.DotNetConsole;

namespace DotNetConsoleSdk.Component.CommandLine.Commands.FileSystem
{
    public class DirectoryPath : FileSystemPath
    {
        public DirectoryInfo DirectoryInfo { get; protected set; }

        public DirectoryPath(string path) : base(new DirectoryInfo(path))
        {
            DirectoryInfo = (DirectoryInfo)FileSystemInfo;
        }

        public override bool CheckExists(bool dumpError=true)
        {
            if (!DirectoryInfo.Exists)
            {
                if (dumpError)
                    Errorln($"directory doesn't exists: {this}");
                return false;
            }
            return true;
        }

        public override string ToString()
        {
            return DirectoryInfo.FullName;
        }
    }
}
