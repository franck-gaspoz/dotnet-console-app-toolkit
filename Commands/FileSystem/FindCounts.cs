using System;
using System.Collections.Generic;
using System.Text;

namespace DotNetConsoleSdk.Commands.FileSystem
{
    public class FindCounts
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
}
