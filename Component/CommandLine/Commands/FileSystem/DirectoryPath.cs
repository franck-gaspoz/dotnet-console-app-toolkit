using DotNetConsoleSdk.Component.CommandLine.CommandModel;
using System.IO;
using static DotNetConsoleSdk.DotNetConsole;

namespace DotNetConsoleSdk.Component.CommandLine.Commands.FileSystem
{
    [CustomParamaterType]
    public class DirectoryPath
    {
        public readonly DirectoryInfo DirectoryInfo;

        public DirectoryPath(string path)
        {
            DirectoryInfo = new DirectoryInfo(path);
        }

        public bool CheckExists(bool dumpError=true)
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
