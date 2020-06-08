using DotNetConsoleSdk.Component.CommandLine.CommandModel;
using System.Collections.Generic;
using static DotNetConsoleSdk.DotNetConsole;
using System.IO;
using System;
using static DotNetConsoleSdk.Lib.Str;

namespace DotNetConsoleSdk.Component.CommandLine.Commands.FileSystem
{
    [Commands("commands related to files,directories,mounts/filesystems and disks")]
    public class FileSystemCommands
    {
        [Command("search for files and/or folders")]
        public List<FileSystemPath> Find(
            [Parameter("search path")] DirectoryPath path,
            [Option("p", "name that matches the pattern", true, true)] string pattern,
            [Option("f","check pattern on fullname instead of name")] bool checkPatternOnFullName,
            [Option("i", "files that contains the string", true, true)] string contains,
            [Option("a", "print file system attributes")] bool attributes,
            [Option("s","print short pathes")] bool shortPathes,
            [Option("all", "select files and directories")] bool all,
            [Option("d", "select only directories")] bool dirs,
            [Option("t", "top directory only")] bool top
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
        
        List<FileSystemPath> FindItems(string path, string pattern,bool top,bool all,bool dirs,bool attributes,bool shortPathes,string contains,bool checkPatternOnFullName,FindCounts counts)
        {
            var dinf = new DirectoryInfo(path);
            List<FileSystemPath> items = new List<FileSystemPath>();
            bool hasPattern = !string.IsNullOrWhiteSpace(pattern);
            bool hasContains = !string.IsNullOrWhiteSpace(contains);
            
            if (CommandLineProcessor.CancellationTokenSource.Token.IsCancellationRequested) 
                return items;

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

                    if (CommandLineProcessor.CancellationTokenSource.Token.IsCancellationRequested) 
                        return items;
                }
                return items;
            } catch (UnauthorizedAccessException)
            {
                Errorln($"unauthorized access to {new DirectoryPath(path).FileSystemInfo.FullName}");
                return items;
            }
        }

        [Command("list files and folders in a path. eventually recurse in sub pathes")]
        public List<FileSystemPath> Dir(
            [Parameter("path where to list files and folders. if not specified is equal to the current directory",true)] WildcardFilePath path,
            [Option("na", "do not print file system attributes")] bool noattributes,
            [Option("r", "recurses in sub pathes")] bool recurse
            )
        {
            var r = new List<FileSystemPath>();
            path ??= new WildcardFilePath(Environment.CurrentDirectory);
            if (path.CheckExists())
            {
                var dinf = new DirectoryInfo(path.FileSystemInfo.FullName);
                var scan = dinf.GetFileSystemInfos("*",recurse?SearchOption.AllDirectories:SearchOption.TopDirectoryOnly);
                foreach ( var fsinf in scan )
                {
                    var sitem = FileSystemPath.Get(fsinf);
                    sitem.Print(!noattributes, !recurse, "", Br);
                }
            }
            return r;
        }
    }
}
