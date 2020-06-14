﻿using DotNetConsoleAppToolkit.Component.CommandLine;
using System;
using System.Data;
using static DotNetConsoleAppToolkit.DotNetConsole;
using cons = DotNetConsoleAppToolkit.DotNetConsole;

namespace DotNetConsoleAppToolkit.Console
{
    public class PrintPrimitives
    {
        public static void Print(DataTable table)
        {
            EnableFillLineFromCursor = false;
            HideCur();
            var colLengths = new int[table.Columns.Count];
            foreach ( var rw in table.Rows )
            {
                var cols = ((DataRow)rw).ItemArray;
                for (int i = 0; i < cols.Length; i++)
                {
                    var s = cols[i]?.ToString() ?? "";
                    colLengths[i] = Math.Max(s.Length, colLengths[i]);
                    colLengths[i] = Math.Max(table.Columns[i].ColumnName.Length, colLengths[i]);
                }
            }
            var colsep = ColorSettings.TableBorder+" | "+ColorSettings.Default;
            var colseplength = 3;
            var tablewidth = 3;
            for (int i = 0; i < table.Columns.Count; i++)
                tablewidth += table.Columns[i].ColumnName.PadRight(colLengths[i], ' ').Length + colseplength;
            var line = ColorSettings.TableBorder + "".PadRight(tablewidth, '-');

            Println(line);
            for (int i=0;i<table.Columns.Count;i++)
            {
                if (i == 0) cons.Print(colsep);
                var col = table.Columns[i];
                var colName = col.ColumnName.PadRight(colLengths[i], ' ');
                cons.Print(ColorSettings.TableColumnName + colName+colsep);
            }
            Println(Br + line);

            foreach ( var rw in table.Rows )
            {
                if (CommandLineProcessor.CancellationTokenSource.IsCancellationRequested)
                {
                    EnableFillLineFromCursor = true;
                    ShowCur();
                    Println(ColorSettings.Default + "");
                    return;
                }
                var row = (DataRow)rw;
                var arr = row.ItemArray;
                for (int i=0;i<arr.Length;i++)
                {
                    if (i == 0) cons.Print(colsep);
                    cons.Print(ColorSettings.Default+arr[i].ToString().PadRight(colLengths[i],' ')+colsep);
                }
                Println();
            }
            Println(line+ColorSettings.Default.ToString());
            ShowCur();
            EnableFillLineFromCursor = true;
        }
    }
}