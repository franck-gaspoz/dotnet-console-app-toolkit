using DotNetConsoleSdk.Component.CommandLine.CommandModel;
using System.IO;
using static DotNetConsoleSdk.Lib.Str;
using static DotNetConsoleSdk.DotNetConsole;
using System.Globalization;

namespace DotNetConsoleSdk.Component.CommandLine.Commands.FileSystem
{
    [CustomParamaterType]
    public abstract class FileSystemPath
    {
        public static string ErrorColorization = $"{Red}";
        public static string DirectoryColorization = $"{Bdarkgreen}";
        public static string FileColorization = $"";
        public readonly FileSystemInfo FileSystemInfo;

        public string Error;

        public FileSystemPath(FileSystemInfo fileSystemInfo)
        {
            FileSystemInfo = fileSystemInfo;
        }

        public abstract bool CheckExists(bool dumpError = true);

        public bool IsDirectory => FileSystemInfo.Attributes.HasFlag(FileAttributes.Directory);
        public bool IsFile => !IsDirectory;
        public bool HasError => Error != null;

        public string GetError() => $"{ErrorColorization}{Error}";

        public static FileSystemPath Get(FileSystemInfo fsinf)
        {
            if (fsinf.Attributes.HasFlag(FileAttributes.Directory))
                return new DirectoryPath(fsinf.FullName);
            else
                return new FilePath(fsinf.FullName);
        }

        public void Print(bool printAttributes=false,bool shortPath=false,string prefix="",string postfix="")
        {
            var color = (IsDirectory) ? DirectoryColorization : FileColorization;
            var r = "";
            var attr = "";
            if (printAttributes)
            {
                var dir = IsDirectory ? " d" : "  ";
                var size = (IsDirectory) ? "" : HumanFormatOfSize(((FileInfo)FileSystemInfo).Length, 2);
                var moddat = FileSystemInfo.LastWriteTime;
                var smoddat = $"{moddat.ToString("MMM", CultureInfo.InvariantCulture),-3} {moddat.Day,-2} {moddat.Hour.ToString().PadLeft(2,'0')}:{moddat.Minute.ToString().PadLeft(2,'0')}";
                attr = $"{dir} {size,10} {smoddat}  ";
            }
            r += $"{attr}{color}{prefix}{(shortPath?FileSystemInfo.Name:FileSystemInfo.FullName)}{postfix}";
            DotNetConsole.Print(r);
            if (HasError)
                DotNetConsole.Print($" {ErrorColorization}{GetError()}");
        }
    }
}
