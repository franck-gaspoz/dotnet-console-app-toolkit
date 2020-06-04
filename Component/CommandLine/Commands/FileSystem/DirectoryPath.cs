using DotNetConsoleSdk.Component.CommandLine.CommandModel;
using System.IO;

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

        public override string ToString()
        {
            return DirectoryInfo.FullName;
        }
    }
}
