using System;
using System.Data;
using System.Threading;
using static DotNetConsoleAppToolkit.DotNetConsole;

namespace DotNetConsoleAppToolkit.Console
{
    public class PrintPrimitives
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

            if (!noBorders) @out.Println(line);
            for (int i=0;i<table.Columns.Count;i++)
            {
                if (i == 0) @out.Print(colsep);
                var col = table.Columns[i];
                var colName = col.ColumnName.PadRight(colLengths[i], ' ');
                @out.Print(ColorSettings.TableColumnName + colName+colsep);
            }
            @out.Println();
            if (!noBorders) @out.Println(line);

            foreach ( var rw in table.Rows )
            {
                if (cancellationTokenSource.IsCancellationRequested)
                {
                    @out.EnableFillLineFromCursor = true;
                    @out.ShowCur();
                    @out.Println(ColorSettings.Default + "");
                    return;
                }
                var row = (DataRow)rw;
                var arr = row.ItemArray;
                for (int i=0;i<arr.Length;i++)
                {
                    if (i == 0) Out.Print(colsep);
                    var txt = (arr[i]==null)?"":arr[i].ToString();
                    var l = Out.GetPrint(txt).Length;
                    @out.Print(ColorSettings.Default+txt+("".PadRight(Math.Max(0,colLengths[i]-l),' '))+colsep);
                }
                @out.Println();
            }
            @out.Println(line+ColorSettings.Default.ToString());
            @out.ShowCur();
            @out.EnableFillLineFromCursor = true;
        }
    }
}
