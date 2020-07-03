using DotNetConsoleAppToolkit.Console;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using prim = DotNetConsoleAppToolkit.Console.PrintPrimitives;

namespace DotNetConsoleAppToolkit.Lib
{
    public static class TypesExt
    {
        public static void Merge<T>(this List<T> mergeInto,List<T> merged)
        {
            foreach (var o in merged)
                if (!mergeInto.Contains(o))
                    mergeInto.Add(o);
        } 

        public static void AddColumns(this DataTable table,params string[] columnNames)
        {
            foreach (var colName in columnNames)
                table.Columns.Add(colName);
        }

        public static void Print(this DataTable x, ConsoleTextWriterWrapper @out, CancellationTokenSource cancellationTokenSource, bool noBorders = false) => prim.Print(@out, cancellationTokenSource, x, noBorders);
        public static void Print(this string x, ConsoleTextWriterWrapper @out) => @out.Print(x);
        public static void Print(this int x, ConsoleTextWriterWrapper @out) => @out.Print(x);
        public static void Print(this double x, ConsoleTextWriterWrapper @out) => @out.Print(x);
        public static void Print(this float x, ConsoleTextWriterWrapper @out) => @out.Print(x);
        public static void Print(this bool x, ConsoleTextWriterWrapper @out) => @out.Print(x);
        public static void Println(this string x, ConsoleTextWriterWrapper @out) => @out.Println(x);
        public static void Println(this int x, ConsoleTextWriterWrapper @out) => @out.Println(x+"");
        public static void Println(this double x, ConsoleTextWriterWrapper @out) => @out.Println(x+"");
        public static void Println(this float x, ConsoleTextWriterWrapper @out) => @out.Println(x+"");
        public static void Println(this bool x, ConsoleTextWriterWrapper @out) => @out.Println(x+"");
    }
}
