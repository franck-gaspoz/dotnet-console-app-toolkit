using System;
using System.Data;
using System.Threading;
using static DotNetConsoleAppToolkit.DotNetConsole;

namespace DotNetConsoleAppToolkit.Console
{
    public class EchoPrimitives
    {
        public static void Print(
            ConsoleTextWriterWrapper @out,
            CancellationTokenSource cancellationTokenSource,
            DataTable table,
            bool noBorders=false)
        {
            @out.EnableFillLineFromCursor = false;
            @out.HideCur();
            var colLengths = new int[table.Columns.Count];
            foreach ( var rw in table.Rows )
            {
                var cols = ((DataRow)rw).ItemArray;
                for (int i = 0; i < cols.Length; i++)
                {
                    var s = @out.GetPrint(cols[i]?.ToString()) ?? "";
                    colLengths[i] = Math.Max(s.Length, colLengths[i]);
                    colLengths[i] = Math.Max(table.Columns[i].ColumnName.Length, colLengths[i]);
                }
            }
            var colsep = noBorders ? " " : (ColorSettings.TableBorder + " | " + ColorSettings.Default);
            var colseplength = noBorders?0:3;
            var tablewidth = noBorders ? 0 : 3;
            for (int i = 0; i < table.Columns.Count; i++)
                tablewidth += table.Columns[i].ColumnName.PadRight(colLengths[i], ' ').Length + colseplength;
            var line = noBorders ? "" : (ColorSettings.TableBorder + "".PadRight(tablewidth, '-'));

            if (!noBorders) @out.Echoln(line);
            for (int i=0;i<table.Columns.Count;i++)
            {
                if (i == 0) @out.Echo(colsep);
                var col = table.Columns[i];
                var colName = col.ColumnName.PadRight(colLengths[i], ' ');
                @out.Echo(ColorSettings.TableColumnName + colName+colsep);
            }
            @out.Echoln();
            if (!noBorders) @out.Echoln(line);

            foreach ( var rw in table.Rows )
            {
                if (cancellationTokenSource.IsCancellationRequested)
                {
                    @out.EnableFillLineFromCursor = true;
                    @out.ShowCur();
                    @out.Echoln(ColorSettings.Default + "");
                    return;
                }
                var row = (DataRow)rw;
                var arr = row.ItemArray;
                for (int i=0;i<arr.Length;i++)
                {
                    if (i == 0) Out.Echo(colsep);
                    var txt = (arr[i]==null)?"":arr[i].ToString();
                    var l = Out.GetPrint(txt).Length;
                    @out.Echo(ColorSettings.Default+txt+("".PadRight(Math.Max(0,colLengths[i]-l),' '))+colsep);
                }
                @out.Echoln();
            }
            @out.Echoln(line+ColorSettings.Default.ToString());
            @out.ShowCur();
            @out.EnableFillLineFromCursor = true;
        }
    }
}
