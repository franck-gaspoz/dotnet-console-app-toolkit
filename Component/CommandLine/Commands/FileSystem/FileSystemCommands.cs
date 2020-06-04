using DotNetConsoleSdk.Component.CommandLine.CommandModel;
using System.Collections.Generic;
using static DotNetConsoleSdk.DotNetConsole;
using System.IO;
using System;
using static DotNetConsoleSdk.Lib.Str;

namespace DotNetConsoleSdk.Component.CommandLine.Commands.FileSystem
{
    [Commands]
    public class FileSystemCommands
    {
        [Command("search for files and/or folders")]
        public List<string> Find(
            [Parameter("search path")] DirectoryPath path,
            [Option("pat", "name that matches the pattern", true, true)] string pattern,
            [Option("in", "files that contains the string", true, true)] string contains,
            [Option("attr", "print file system attributes")] bool attributes,
            [Option("shp","print short pathes")] bool shortPathes,
            [Option("all", "select files and directories")] bool all,
            [Option("dir", "select only directories")] bool dirs,
            [Option("top", "top directory only")] bool top
            )
        {
            if (path.CheckExists())
            {
                var sp = string.IsNullOrWhiteSpace(pattern) ? "*" : pattern;
                var counts = new FindCounts();
                var items = FindItems(path.DirectoryInfo.FullName, sp, top,all,dirs,attributes,shortPathes,counts);
                Println($"found {Plur("file",counts.FilesCount)} in {Plur("folder",counts.FoldersCount)}");
            }
            return null;
        }

        class FindCounts
        {
            public int FoldersCount;
            public int FilesCount;
        }

        List<FileSystemPath> FindItems(string path, string pattern,bool top,bool all,bool dirs,bool attributes,bool shortPathes,FindCounts counts)
        {
            var dinf = new DirectoryInfo(path);
            List<FileSystemPath> items = new List<FileSystemPath>();
            bool hasPattern = !string.IsNullOrWhiteSpace(pattern);
            
            if (CommandLineProcessor.CancellationTokenSource.Token.IsCancellationRequested) return items;

            try
            {
                var scan = dinf.GetFileSystemInfos();

                foreach ( var fsinf in scan )
                {
                    var sitem = FileSystemPath.Get(fsinf);

                    if (sitem.IsDirectory)
                    {
                        if ((dirs || all) && (!hasPattern || MatchWildcard(pattern, sitem.FileSystemInfo.Name)))
                        {
                            items.Add(sitem);
                            sitem.Print(attributes, shortPathes, "", Br);
                            counts.FoldersCount++;
                        }
                        else
                            sitem = null;

                        if (!top)
                            items.AddRange(FindItems(fsinf.FullName, pattern, top, all, dirs, attributes, shortPathes, counts));
                    }
                    else
                    {
                        if (!dirs && (!hasPattern || MatchWildcard(pattern, sitem.FileSystemInfo.Name)))
                        {
                            counts.FilesCount++;
                            items.Add(sitem);
                            sitem.Print(attributes, shortPathes, "", Br);
                        }
                            sitem = null;
                    }

                    if (CommandLineProcessor.CancellationTokenSource.Token.IsCancellationRequested) return items;
                }
                return items;
            } catch (UnauthorizedAccessException)
            {
                Errorln($"unauthorized access to {new DirectoryPath(path).FileSystemInfo.FullName}");
                return items;
            }
        }
    }
}
