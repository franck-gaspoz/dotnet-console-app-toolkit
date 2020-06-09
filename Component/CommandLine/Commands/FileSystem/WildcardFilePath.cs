﻿using System.IO;
using static DotNetConsoleSdk.DotNetConsole;

namespace DotNetConsoleSdk.Component.CommandLine.Commands.FileSystem
{
    public class WildcardFilePath : DirectoryPath
    {
        public readonly string WildCardFileName;

        public WildcardFilePath(string path) : base(path) { 
            if (ContainsWildcardFileName(path))
            {
                var basepath = Path.GetDirectoryName(path);
                if (string.IsNullOrWhiteSpace(basepath)) basepath = System.Environment.CurrentDirectory;
                WildCardFileName = Path.GetFileName(path);
                FileSystemInfo = new DirectoryInfo(basepath);
            }
            else 
            { 
                if (File.Exists(path))
                {
                    var basepath = Path.GetDirectoryName(path);
                    if (string.IsNullOrWhiteSpace(basepath)) basepath = System.Environment.CurrentDirectory;
                    WildCardFileName = Path.GetFileName(path);
                    FileSystemInfo = new DirectoryInfo(basepath);
                } else
                {
                    if (Directory.Exists(path))
                    {
                        FileSystemInfo = new DirectoryInfo(path);
                    }
                }
            }
            DirectoryInfo = (DirectoryInfo)FileSystemInfo;
        }

        public override bool CheckExists(bool dumpError = true)
        {
            if (!DirectoryInfo.Exists)
            {
                if (dumpError)
                    Errorln($"file or directory doesn't exists: {this}");
                return false;
            }
            return true;
        }

        public static bool ContainsWildcardFileName(string path) {
            var ext = Path.GetFileName(path);
            return ext.Contains('*') || ext.Contains('?');
        }
    }
}

