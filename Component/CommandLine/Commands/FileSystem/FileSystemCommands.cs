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
        public List<FileSystemPath> Find(
            [Parameter("search path")] DirectoryPath path,
            [Option("pat", "name that matches the pattern", true, true)] string pattern,
            [Option("flp","check pattern on fullname instead of name")] bool checkPatternOnFullName,
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
                var items = FindItems(path.DirectoryInfo.FullName, sp, top,all,dirs,attributes,shortPathes,contains, checkPatternOnFullName,counts);
                var f = GetCmd(KeyWords.f+"",DefaultForeground.ToString().ToLower());
                var elapsed = DateTime.Now - counts.BeginDateTime;
                Println($"found {Cyan}{Plur("file",counts.FilesCount,f)} and {Cyan}{Plur("folder",counts.FoldersCount,f)}. scanned {Cyan}{Plur("file",counts.ScannedFilesCount,f)} in {Cyan}{Plur("folder",counts.ScannedFoldersCount,f)} during {TimeSpanDescription(elapsed,Cyan,f)}");
                return items;
            }
            return new List<FileSystemPath>();
        }

        class FindCounts
        {
            public int FoldersCount;
            public int FilesCount;
            public int ScannedFoldersCount;
            public int ScannedFilesCount;
            public DateTime BeginDateTime;
            public FindCounts()
            {
                BeginDateTime = DateTime.Now;
            }
        }

        List<FileSystemPath> FindItems(string path, string pattern,bool top,bool all,bool dirs,bool attributes,bool shortPathes,string contains,bool checkPatternOnFullName,FindCounts counts)
        {
            var dinf = new DirectoryInfo(path);
            List<FileSystemPath> items = new List<FileSystemPath>();
            bool hasPattern = !string.IsNullOrWhiteSpace(pattern);
            bool hasContains = !string.IsNullOrWhiteSpace(contains);
            
            if (CommandLineProcessor.CancellationTokenSource.Token.IsCancellationRequested) return items;

            try
            {
                counts.ScannedFoldersCount++;
                var scan = dinf.GetFileSystemInfos();

                foreach ( var fsinf in scan )
                {
                    var sitem = FileSystemPath.Get(fsinf);

                    if (sitem.IsDirectory)
                    {
                        if ((dirs || all) && (!hasPattern || MatchWildcard(pattern, checkPatternOnFullName ? sitem.FileSystemInfo.FullName : sitem.FileSystemInfo.Name)))
                        {
                            items.Add(sitem);
                            sitem.Print(attributes, shortPathes, "", Br);
                            counts.FoldersCount++;
                        }
                        else
                            sitem = null;

                        if (!top)
                            items.AddRange(FindItems(fsinf.FullName, pattern, top, all, dirs, attributes, shortPathes,contains, checkPatternOnFullName, counts));
                    }
                    else
                    {
                        counts.ScannedFilesCount++;
                        if (!dirs && (!hasPattern || MatchWildcard(pattern, checkPatternOnFullName?sitem.FileSystemInfo.FullName:sitem.FileSystemInfo.Name)))
                        {
                            if (hasContains)
                            {
                                var str = File.ReadAllText(sitem.FileSystemInfo.FullName);
                                if (!str.Contains(contains))
                                    sitem = null;
                            }
                            if (sitem != null)
                            {
                                counts.FilesCount++;
                                items.Add(sitem);
                                sitem.Print(attributes, shortPathes, "", Br);
                            }
                        }
                        else
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
