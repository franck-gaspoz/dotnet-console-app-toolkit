using DotNetConsoleSdk.Component.CommandLine.CommandModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using static DotNetConsoleSdk.DotNetConsole;
using static DotNetConsoleSdk.Lib.Str;
using sc = System.Console;

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
                var items = FindItems(path.FullName, sp, top,all,dirs,attributes,shortPathes,contains, checkPatternOnFullName,counts,true);
                var f = GetCmd(KeyWords.f+"",DefaultForeground.ToString().ToLower());
                var elapsed = DateTime.Now - counts.BeginDateTime;
                if (items.Count > 0) Println();
                Println($"found {Cyan}{Plur("file",counts.FilesCount,f)} and {Cyan}{Plur("folder",counts.FoldersCount,f)}. scanned {Cyan}{Plur("file",counts.ScannedFilesCount,f)} in {Cyan}{Plur("folder",counts.ScannedFoldersCount,f)} during {TimeSpanDescription(elapsed,Cyan,f)}");
                return items;
            }
            return new List<FileSystemPath>();
        }
        
        List<FileSystemPath> FindItems(string path, string pattern,bool top,bool all,bool dirs,bool attributes,bool shortPathes,string contains,bool checkPatternOnFullName,FindCounts counts,bool print,bool alwaysSelectDirs=false)
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
                        if ((dirs || all) && (alwaysSelectDirs || (!hasPattern || MatchWildcard(pattern, checkPatternOnFullName ? sitem.FileSystemInfo.FullName : sitem.FileSystemInfo.Name))))
                        {
                            items.Add(sitem);
                            if (print) sitem.Print(attributes, shortPathes, "", Br);
                            counts.FoldersCount++;
                        }
                        else
                            sitem = null;

                        if (!top)
                            items.AddRange(FindItems(fsinf.FullName, pattern, top, all, dirs, attributes, shortPathes,contains, checkPatternOnFullName, counts, print));
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
                                if (print) sitem.Print(attributes, shortPathes, "", Br);
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
                Errorln($"unauthorized access to {new DirectoryPath(path).PrintableFullName}");
                return items;
            }
        }

        [Command("list files and folders in a path. eventually recurse in sub paths")]
        public List<FileSystemPath> Dir(
            [Parameter("path where to list files and folders. if not specified is equal to the current directory",true)] WildcardFilePath path,
            [Option("na", "do not print file system attributes")] bool noattributes,
            [Option("r", "also list files and folders in sub directories. force display files full path")] bool recurse,
            [Option("w", "displays file names on several columns so output fills console width (only if not recurse mode). disable print of attributes")] bool wide
            )
        {
            var r = new List<FileSystemPath>();
            path ??= new WildcardFilePath(Environment.CurrentDirectory);
            if (path.CheckExists())
            {
                var counts = new FindCounts();
                var items = FindItems(path.FullName, path.WildCardFileName ?? "*", !recurse, true, false, !noattributes, !recurse, null, false, counts, false,true);
                var f = GetCmd(KeyWords.f + "", DefaultForeground.ToString().ToLower());
                long totFileSize = 0;
                var cancellationTokenSource = new CancellationTokenSource();
                if (wide) noattributes = true;
                void postCmd(object o, EventArgs e)
                {
                    sc.CancelKeyPress -= cancelCmd;
                    Println($"{Tab}{Cyan}{Plur("file", counts.FilesCount, f),-30}{HumanFormatOfSize(totFileSize, 2)}");
                    Println($"{Tab}{Cyan}{Plur("folder", counts.FoldersCount, f),-30}{Drive.GetDriveInfo(path.FileSystemInfo.FullName)}");
                }
                void cancelCmd(object o, ConsoleCancelEventArgs e)
                {
                    e.Cancel = true;
                    cancellationTokenSource.Cancel(); 
                }                
                int printResult()
                {
                    var i = 0;

                    int maxitlength = 0;
                    foreach (var item in items)
                    {
                        if (item.IsFile) totFileSize += ((FileInfo)item.FileSystemInfo).Length;
                        maxitlength = Math.Max(item.Name.Length, maxitlength);
                    }
                    maxitlength += 4;
                    var nbcols = Math.Floor((double)(ActualWorkArea.right - ActualWorkArea.left+1)/(double)maxitlength);

                    int nocol = 0;
                    foreach (var item in items)
                    {
                        if (cancellationTokenSource.IsCancellationRequested)
                            return i;
                        item.Print(!noattributes, !recurse, "", (!wide || recurse || nocol == nbcols-1)?Br:"",(wide && !recurse)?maxitlength:-1);
                        i++;
                        nocol++;
                        if (nocol == nbcols)
                            nocol = 0;
                    }
                    if (!recurse && wide && nocol < nbcols && nocol>0) Println();
                    return i;
                }
                sc.CancelKeyPress += cancelCmd;
                var task = Task.Run<int>(() => printResult(),
                    cancellationTokenSource.Token);
                try
                {
                    task.Wait(cancellationTokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                    var res = task.Result;
                }
                postCmd(null,null);
            }
            return r;
        }
       
        [Command("sets the path of the working directory")]
        public void Cd(
            [Parameter("path where to list files and folders. if not specified is equal to the current directory", true)] DirectoryPath path
            )
        {
            path ??= new DirectoryPath(Path.GetPathRoot(Environment.CurrentDirectory));
            if (path.CheckExists())
            {
                var bkpath = Environment.CurrentDirectory;
                try
                {
                    Environment.CurrentDirectory = path.FullName;

                } catch (UnauthorizedAccessException)
                {
                    Errorln($"unauthorized access to {path.PrintableFullName}");
                    Environment.CurrentDirectory = bkpath;
                }
            }
        }

        [Command("print the paht of the current working directory")]
        public void Pwd(
            [Option("na", "do not print file system attributes")] bool noattributes
            )
        {
            var path = new DirectoryPath(Environment.CurrentDirectory);
            if (path.CheckExists())
            {
                path.Print(!noattributes, false, "", Br);
            }
        }
    }
}
