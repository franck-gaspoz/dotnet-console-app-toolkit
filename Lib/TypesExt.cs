using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Data;
using static DotNetConsoleAppToolkit.DotNetConsole;
using prim=DotNetConsoleAppToolkit.Console.PrintPrimitives;

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

        public static void Print(this DataTable x,bool noBorders=false) => prim.Print(x,noBorders);
        public static void Print(this string x) => Print(x);
        public static void Print(this int x) => Print(x);
        public static void Print(this double x) => Print(x);
        public static void Print(this float x) => Print(x);
        public static void Print(this bool x) => Print(x);
        public static void Println(this string x) => Println(x);
        public static void Println(this int x) => Println(x);
        public static void Println(this double x) => Println(x);
        public static void Println(this float x) => Println(x);
        public static void Println(this bool x) => Println(x);
    }
}
