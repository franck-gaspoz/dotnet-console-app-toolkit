using DotNetConsoleAppToolkit.Component.CommandLine;
using DotNetConsoleAppToolkit.Component.CommandLine.CommandModel;
using DotNetConsoleAppToolkit.Component.CommandLine.Parsing;
using DotNetConsoleAppToolkit.Console;
using DotNetConsoleAppToolkit.Lib;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static DotNetConsoleAppToolkit.Console.Interaction;
using static DotNetConsoleAppToolkit.DotNetConsole;
using static DotNetConsoleAppToolkit.Lib.Str;
using sc = System.Console;
using static DotNetConsoleAppToolkit.Lib.FileSystem;

namespace DotNetConsoleAppToolkit.Commands.FileSystem
{
    [Commands("commands related to files,directories,mounts/filesystems and disks")]
    public class FileSystemCommands : CommandsType
    {
        public FileSystemCommands(CommandLineProcessor commandLineProcessor) : base(commandLineProcessor) { }

        [Command("search for files and/or folders")]
        public List<FileSystemPath> Find(
            [Parameter("search path")] DirectoryPath path,
            [Option("p", "select names that matches the pattern", true, true)] string pattern,
            [Option("i", "if set and p is set, perform a non case sensisitive search")] bool ignoreCase,
            [Option("f","check pattern on fullname instead of name")] bool checkPatternOnFullName,
            [Option("c", "files that contains the string", true, true)] string contains,
            [Option("a", "print file system attributes")] bool attributes,
            [Option("s","print short pathes")] bool shortPathes,
            [Option("all", "select files and directories")] bool all,
            [Option("d", "select only directories")] bool dirs,
            [Option("t", "search in top directory only")] bool top
            )
        {
            if (path.CheckExists())
            {
                var sp = string.IsNullOrWhiteSpace(pattern) ? "*" : pattern;
                var counts = new FindCounts();
                var items = FindItems(path.FullName, sp, top,all,dirs,attributes,shortPathes,contains, checkPatternOnFullName,counts,true,false, ignoreCase);
                var f = DefaultForegroundCmd;
                var elapsed = DateTime.Now - counts.BeginDateTime;
                if (items.Count > 0) Out.Println();
                Out.Println($"found {ColorSettings.Numeric}{Plur("file",counts.FilesCount,f)} and {ColorSettings.Numeric}{Plur("folder",counts.FoldersCount,f)}. scanned {ColorSettings.Numeric}{Plur("file",counts.ScannedFilesCount,f)} in {ColorSettings.Numeric}{Plur("folder",counts.ScannedFoldersCount,f)} during {TimeSpanDescription(elapsed, ColorSettings.Numeric.ToString(), f)}");
                return items;
            }
            return new List<FileSystemPath>();
        }
        
        /*
        public static List<FileSystemPath> FindItems(string path, string pattern,bool top,bool all,bool dirs,bool attributes,bool shortPathes,string contains,bool checkPatternOnFullName,FindCounts counts,bool print,bool alwaysSelectDirs=false)
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
                                try
                                {
                                    var str = File.ReadAllText(sitem.FileSystemInfo.FullName);
                                    if (!str.Contains(contains))
                                        sitem = null;
                                } catch (Exception ex)
                                {
                                    Errorln($"file read error: {ex.Message} when accessing file: {sitem.PrintableFullName}");
                                }
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
        */

        [Command("list files and folders in a path. eventually recurse in sub paths")]
        public List<FileSystemPath> Dir(
            [Parameter("path where to list files and folders. if not specified is equal to the current directory. use wildcards * and ? to filter files and folders names",true)] WildcardFilePath path,
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
                var items = FindItems(path.FullName, path.WildCardFileName ?? "*", !recurse, true, false, !noattributes, !recurse, null, false, counts, false,false);
                var f = DefaultForegroundCmd;
                long totFileSize = 0;
                var cancellationTokenSource = new CancellationTokenSource();
                if (wide) noattributes = true;
                void postCmd(object o, EventArgs e)
                {
                    sc.CancelKeyPress -= cancelCmd;
                    Out.Println($"{Tab}{ColorSettings.Numeric}{Plur("file", counts.FilesCount, f),-30}{HumanFormatOfSize(totFileSize, 2," ", ColorSettings.Numeric.ToString(), f)}");
                    Out.Println($"{Tab}{ColorSettings.Numeric}{Plur("folder", counts.FoldersCount, f),-30}{Drives.GetDriveInfo(path.FileSystemInfo.FullName,false, ColorSettings.Numeric.ToString(), f," ",2)}");
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
                    counts.FilesCount = counts.FoldersCount = 0;
                    foreach (var item in items)
                    {
                        if (item.IsFile)
                        {
                            totFileSize += ((FileInfo)item.FileSystemInfo).Length;
                            counts.FilesCount++;
                        }
                        else
                            counts.FoldersCount++;
                        maxitlength = Math.Max(item.Name.Length, maxitlength);
                    }
                    maxitlength += 4;
                    var (id,left,top,right,bottom) = DotNetConsole.ActualWorkArea();
                    var nbcols = Math.Floor((double)(right - left+1)/(double)maxitlength);

                    int nocol = 0;
                    foreach (var item in items)
                    {
                        if (cancellationTokenSource.IsCancellationRequested)
                            return i;

                        item.Print(!noattributes, !recurse, "", (!wide || recurse || nocol == nbcols - 1) ? Br : "", (wide && !recurse) ? maxitlength : -1);
                        i++;
                        nocol++;
                        if (nocol == nbcols)
                            nocol = 0;
                    }
                    if (!recurse && wide && nocol < nbcols && nocol>0) Out.Println();
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

        [Command("print the path of the current working directory")]
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

        [Command("print informations about drives/mount points")]
        public void Driveinfo(
            [Parameter("drive name for which informations must be printed. if no drive specified, list all drives",true)] string drive,
            [Option("b", "if set add table borders")] bool borders
            )
        {
            var drives = DriveInfo.GetDrives().AsQueryable();
            if (drive!=null)
            {
                drives = drives.Where(x => x.Name.Equals(drive, CommandLineParser.SyntaxMatchingRule));
                if (drives.Count()==0) {
                    Errorln($"drive \"{drive}\" not found");
                }
            }
            var table = new DataTable();
            table.AddColumns("name","label","type","format","bytes");
            foreach ( var di in drives )
            {
                var f = DefaultForegroundCmd;
                var row = table.NewRow();
                try
                {
                    row["name"] = $"{ColorSettings.Highlight}{di.Name}{f}";
                    row["label"] = $"{ColorSettings.Highlight}{di.VolumeLabel}{f}";
                    row["type"] = $"{ColorSettings.Name}{di.DriveType}{f}";
                    row["format"] = $"{ColorSettings.Name}{di.DriveFormat}{f}";
                    row["bytes"] = (di.TotalSize==0)?"": $"{HumanFormatOfSize(di.TotalFreeSpace, 2, " ", ColorSettings.Numeric.ToString(), f)}{f}/{ColorSettings.Numeric}{HumanFormatOfSize(di.TotalSize, 2, " ", ColorSettings.Numeric.ToString(), f)} {f}({ColorSettings.Highlight}{Math.Round((double)di.TotalFreeSpace / (double)di.TotalSize * 100d, 2)}{f} %)";
                } catch (UnauthorizedAccessException) {
                    Errorln($"unauthorized access to drive {di.Name}");
                    row["name"] = $"{ColorSettings.Highlight}{di.Name}{f}";
                    row["label"] = "?";
                    row["type"] = "?";
                    row["format"] = "?";
                    row["bytes"] = "?";
                }
                catch (Exception ex)
                {
                    Errorln($"error when accessing drive {di.Name}: {ex.Message}");
                    row["name"] = $"{ColorSettings.Highlight}{di.Name}{f}";
                    row["label"] = "?";
                    row["type"] = "?";
                    row["format"] = "?";
                    row["bytes"] = "?";
                }
                table.Rows.Add(row);
            }
            table.Print(!borders);
        }

        [Command("remove file(s) and/or the directory(ies)")]
        public List<string> Rm(
            [Parameter("file or folder path")] WildcardFilePath path,
            [Option("r", "also remove files and folders in sub directories")] bool recurse,
            [Option("i","prompt before any removal")] bool interactive,
            [Option("v", "explain what is being done")] bool verbose,
            [Option("d", "remove empty directories")] bool rmEmptyDirs,
            [Option("na", "do not print file system attributes when verbose")] bool noattributes,
            [Option("s", "don't remove any file/or folder, just simulate the operation (enable verbose)")] bool simulate
        )
        {
            var r = new List<string>();
            if (path.CheckExists())
            {
                var counts = new FindCounts();
                var items = FindItems(path.FullName, path.WildCardFileName ?? "*", !recurse, true, false, !noattributes, !recurse, null, false, counts, false, false);
                var cancellationTokenSource = new CancellationTokenSource();
                verbose |= simulate;
                void cancelCmd(object o, ConsoleCancelEventArgs e)
                {
                    e.Cancel = true;
                    cancellationTokenSource.Cancel();
                };
                void postCmd(object o, EventArgs e)
                {
                    sc.CancelKeyPress -= cancelCmd;
                }
                List<string> processRemove()
                {
                    var r = new List<string>();
                    foreach ( var item in items )
                    {
                        if (cancellationTokenSource.IsCancellationRequested) return r;
                        bool deleted = false;
                        if (item.IsFile)
                        {
                            if (item.FileSystemInfo.Exists && !r.Contains(item.FullName))
                            {
                                if (interactive)
                                {
                                    if (Confirm("rm: remove file " + item.GetPrintableName(recurse)) && !simulate)
                                    {
                                        if (!simulate) item.FileSystemInfo.Delete();
                                        deleted = true;
                                    }    
                                }
                                else
                                {
                                    if (!simulate) item.FileSystemInfo.Delete();
                                    deleted = true;
                                }
                            }
                        } else
                        {
                            var dp = (DirectoryPath)item;
                            if ((rmEmptyDirs && dp.IsEmpty) || recurse)
                            {
                                if (dp.DirectoryInfo.Exists && !r.Contains(dp.FullName))
                                {
                                    if (interactive)
                                        r.Merge(RecurseInteractiveDeleteDir(dp, simulate, noattributes, verbose, cancellationTokenSource));
                                    else
                                    {
                                        if (!simulate) dp.DirectoryInfo.Delete(recurse);
                                        deleted = true;
                                    }
                                }
                            }
                        }
                        if (deleted)
                        {
                            if (verbose) item.Print(!noattributes, !recurse, "", Br, -1, "removed ");
                            r.Add(item.FullName);
                        }
                    }
                    return r;
                };
                sc.CancelKeyPress += cancelCmd;
                var task = Task.Run<List<string>>(() => processRemove(), cancellationTokenSource.Token);
                try
                {
                    task.Wait(cancellationTokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                    r = task.Result;
                }
                postCmd(null, null);
            }
            return r;
        }

        [Command("move or rename files and directories" ,
@"- if multiple source, move to a directory that must exists
- if source is a file or a directory and dest is an existing directory move the source
- if source and target are a file that exists remame the source and replace the dest
- if dest doesn't exists rename the source that must be a file or a directory")]
        public void Mv(
            [Parameter("source: file/directory or several corresponding to a wildcarded path")] WildcardFilePath source,
            [Parameter(1,"destination: a file or a directory")] FileSystemPath dest,
            [Option("i","prompt before overwrite")] bool interactive,
            [Option("v","explain what is being done")] bool verbose
            )
        {
            if (source.CheckExists())
            {
                var counts = new FindCounts();
                var items = FindItems(source.FullName, source.WildCardFileName ?? "*", true, true, false,true, false, null, false, counts, false, false);
                var sourceCount = items.Count;
                if (sourceCount > 1)
                {
                    if (dest.CheckExists())
                    {
                        if (!dest.IsDirectory)
                            Errorln("dest must be a directory");
                        else
                        {
                            // move multiple source to dest
                            foreach ( var item in items )
                            {
                                var msg = $"move {item.GetPrintableName()} to {dest.GetPrintableName()}";
                                if (!interactive || Confirm("mv: " + msg))
                                {
                                    if (source.IsFile)
                                        File.Move(item.FullName, Path.Combine(dest.FullName,item.Name));
                                    else
                                        Directory.Move(item.FullName, Path.Combine(dest.FullName, item.Name));
                                    if (verbose) Out.Println(msg.Replace("move ", "moved "));
                                }
                            }
                        }
                    }
                } else
                {
                    if (dest.CheckExists(false))
                    {
                        if (dest.IsDirectory)
                        {
                            // move one source to dest
                            var msg = $"move {source.GetPrintableNameWithWlidCard()} to {dest.GetPrintableName()}";
                            if (!interactive || Confirm("mv: " + msg))
                            {
                                if (source.IsFile)
                                    File.Move(source.FullNameWithWildcard, Path.Combine(dest.FullName, source.NameWithWildcard));
                                else
                                    Directory.Move(source.FullName, Path.Combine(dest.FullName, source.NameWithWildcard));
                                if (verbose) Out.Println(msg.Replace("move ", "moved "));
                            }
                        } else
                        {
                            // rename source (file) to dest (overwrite dest)
                            var msg = $"rename {source.GetPrintableNameWithWlidCard()} to {dest.GetPrintableName()}";
                            if (!interactive || Confirm("mv: "+msg))
                            {
                                dest.FileSystemInfo.Delete();
                                File.Move(source.FullNameWithWildcard, dest.FullName );
                                if (verbose) Out.Println(msg.Replace("rename ", "renamed "));
                            }
                        }
                    } else
                    {
                        // rename source to dest
                        var msg = $"rename {source.GetPrintableNameWithWlidCard()} to {dest.GetPrintableName()}";
                        if (!interactive || Confirm("mv: " + msg))
                        {
                            if (source.IsFile)
                                File.Move(source.FullNameWithWildcard, dest.FullName);
                            else
                                Directory.Move(source.FullName, dest.FullName);
                            if (verbose) Out.Println(msg.Replace("rename ", "renamed "));
                        }
                    }
                }
            }
        }

        List<string> RecurseInteractiveDeleteDir(DirectoryPath dir,bool simulate,bool noattributes,bool verbose,CancellationTokenSource cancellationTokenSource)
        {
            var fullname = true;
            var r = new List<string>();
            verbose |= simulate;
            if (cancellationTokenSource.IsCancellationRequested) return r;
            if (dir.IsEmpty || Confirm("rm: descend "+dir.GetPrintableName(fullname)))
            {
                foreach ( var subdir in dir.DirectoryInfo.EnumerateDirectories())
                    r.Merge(RecurseInteractiveDeleteDir(new DirectoryPath(subdir.FullName), simulate, noattributes, verbose, cancellationTokenSource));
                foreach ( var subfile in dir.DirectoryInfo.EnumerateFiles())
                {
                    var subfi = new FilePath(subfile.FullName);
                    if (Confirm("rm: remove file "+subfi.GetPrintableName(fullname)))
                    {
                        if (!simulate) subfi.FileSystemInfo.Delete();
                        if (verbose) subfi.Print(!noattributes, false, "", Br, -1, "removed ");
                        r.Add(subfi.FullName);
                    }
                }
                if (Confirm("rm: remove directory "+dir.GetPrintableName(fullname)))
                {
                    if (!simulate) dir.DirectoryInfo.Delete(true);
                    if (verbose) dir.Print(!noattributes, false, "", Br, -1, "removed ");
                    r.Add(dir.FullName);
                }
            }
            return r;
        }
    }
}
