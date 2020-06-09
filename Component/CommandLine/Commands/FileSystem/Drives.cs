using System;
using System.IO;
using System.Linq;
using static DotNetConsoleSdk.Lib.Str;

namespace DotNetConsoleSdk.Component.CommandLine.Commands.FileSystem
{
    public class Drives
    {
        public static string GetCurrentDriveInfo() => GetDriveInfo(Environment.CurrentDirectory,true);

        public static string GetDriveInfo(string path,bool printFileSystemInfo=false)
        {
            var rootDirectory = Path.GetPathRoot(path.ToLower());
            var di = DriveInfo.GetDrives().Where(x => x.RootDirectory.FullName.ToLower() == rootDirectory).FirstOrDefault();
            return (di == null) ? "?" : $"{(printFileSystemInfo?(rootDirectory+" "):"")}{HumanFormatOfSize(di.AvailableFreeSpace, 0, "")}/{HumanFormatOfSize(di.TotalSize, 0, "")}{(printFileSystemInfo?$"({di.DriveFormat})":"")}";
        }
    }
}
