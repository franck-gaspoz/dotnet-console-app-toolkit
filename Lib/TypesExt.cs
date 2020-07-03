using DotNetConsoleAppToolkit.Console;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Threading;
using prim = DotNetConsoleAppToolkit.Console.EchoPrimitives;

namespace DotNetConsoleAppToolkit.Lib
{
    public static class TypesExt
    {
        /*public static object GetValue(this MemberInfo memberInfo,object target)
        {
            if (memberInfo is FieldInfo fi)
            {
                return fi.GetValue(target);
            } else
            {
                if (memberInfo is PropertyInfo pi)
                {
                    return pi.GetValue(target);
                }
            }
            return null;
        }*/

        public static void AddOrReplace<TK,TV>(this Dictionary<object,TV> dic,string key,TV value)
        {
            if (dic.ContainsKey(key))
                dic[key] = value;
            else
                dic.Add(key, value);
        }

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
        public static void Print(this string x, ConsoleTextWriterWrapper @out) => @out.Echo(x);
        public static void Print(this int x, ConsoleTextWriterWrapper @out) => @out.Echo(x);
        public static void Print(this double x, ConsoleTextWriterWrapper @out) => @out.Echo(x);
        public static void Print(this float x, ConsoleTextWriterWrapper @out) => @out.Echo(x);
        public static void Print(this bool x, ConsoleTextWriterWrapper @out) => @out.Echo(x);
        public static void Println(this string x, ConsoleTextWriterWrapper @out) => @out.Echoln(x);
        public static void Println(this int x, ConsoleTextWriterWrapper @out) => @out.Echoln(x+"");
        public static void Println(this double x, ConsoleTextWriterWrapper @out) => @out.Echoln(x+"");
        public static void Println(this float x, ConsoleTextWriterWrapper @out) => @out.Echoln(x+"");
        public static void Println(this bool x, ConsoleTextWriterWrapper @out) => @out.Echoln(x+"");
    }
}
