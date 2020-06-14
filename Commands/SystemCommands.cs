using DotNetConsoleAppToolkit.Component.CommandLine.CommandModel;
using DotNetConsoleAppToolkit.Console;
using System;
using System.Data;
using System.Diagnostics;
using static DotNetConsoleAppToolkit.Console.PrintPrimitives;
using static DotNetConsoleAppToolkit.DotNetConsole;
using static DotNetConsoleAppToolkit.Lib.Str;

namespace DotNetConsoleAppToolkit.Component.CommandLine.Commands
{
    [Commands("system commands")]
    public class SystemCommands : CommandsType
    {
        public SystemCommands(CommandLineProcessor commandLineProcessor) : base(commandLineProcessor) { }

        [Command("print a report of current processes")]
        public void Ps(
            [Option("sid", "filter by session id", true, true)] int fsid = -1
            )
        {
            var maxnamel = 40;
            var processes = Process.GetProcesses();
            var table = new DataTable();
            var cpid = table.Columns.Add("pid");
            var sid = table.Columns.Add("sid");
            var cname = table.Columns.Add("name");
            var cbp = table.Columns.Add("prio");
            var cws = table.Columns.Add("ws");
            var cpgms = table.Columns.Add("pgms");
            //var ctpt = table.Columns.Add("tpt");
            //var cvms = table.Columns.Add("vms");
            //var ctitle = table.Columns.Add("window title");
            //var cmname = table.Columns.Add("Module name");
            foreach ( var process in processes )
            {
                var select = true;
                select &= (fsid==-1 || process.SessionId == fsid);

                var row = table.NewRow();
                row[cpid] = process.Id;
                row[sid] = process.SessionId;
                var n = process.ProcessName;
                if (n.Length > maxnamel) n = n.Substring(0, maxnamel) + "...";
                row[cname] = n;
                //row[cmname] = process.MainModule.FileName;
                //row[cvms] = HumanFormatOfSize(process.VirtualMemorySize64, 2);
                row[cpgms] = HumanFormatOfSize(process.PagedMemorySize64, 2);
                row[cws] = HumanFormatOfSize(process.WorkingSet64, 2);
                row[cbp] = process.BasePriority;
                //row[ctpt] = process.TotalProcessorTime+" %";
                //row[ctitle] = process.MainWindowTitle;

                if (select) table.Rows.Add(row);
            }
            Print(table);
        }

        [Command("get information about current user")]
        public void Whoami()
        {
            Println($"{Environment.UserName} [{ColorSettings.Highlight}{Environment.UserDomainName}{ColorSettings.Default}]");
        }
    }
}
