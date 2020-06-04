using DotNetConsoleSdk.Component.CommandLine.CommandModel;
using System.Collections.Generic;
using static DotNetConsoleSdk.DotNetConsole;
using System.IO;
using System;

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
            [Option("file", "select only files")] bool files,
            [Option("dir", "select only directories")] bool dirs,
            [Option("top", "top directory only")] bool top
            )
        {
            if (path.CheckExists())
            {
                var sp = string.IsNullOrWhiteSpace(pattern) ? "" : pattern;
                var items = FindItems(path.DirectoryInfo.FullName, sp, top,files,dirs,attributes);
            }
            return null;
        }

        List<FileSystemPath> FindItems(string path, string pattern,bool top,bool files,bool dirs,bool attributes)
        {
            var dinf = new DirectoryInfo(path);
            List<FileSystemPath> items = new List<FileSystemPath>();

            if (CommandLineProcessor.CancellationTokenSource.Token.IsCancellationRequested) return items;

            try
            {
                var scan = dinf.GetFileSystemInfos(pattern, SearchOption.TopDirectoryOnly );
                foreach ( var fsinf in scan )
                {
                    var fspath = FileSystemPath.Get(fsinf);
                    FileSystemPath sitem = null;
                    if (files && fspath.IsFile) sitem = fspath;
                    if (dirs && fspath.IsDirectory) sitem = fspath;
                    if (!files && !dirs) sitem = fspath;
                    if (sitem != null)
                    {
                        if (!top && sitem.IsDirectory)
                            items.AddRange(FindItems(fsinf.FullName, pattern, top, files, dirs,attributes));
                        else
                            items.Add(sitem);
                        sitem.Print(attributes, "", Br);

                        if (CommandLineProcessor.CancellationTokenSource.Token.IsCancellationRequested) return items;
                    }
                }
                return items;
            } catch (UnauthorizedAccessException)
            {
                var unautdir = new DirectoryPath(path) { Error = "unauthorized access" };
                items.Add(unautdir);
                unautdir.Print(attributes);
                Println();
                return items;
            }
        }
    }
}
