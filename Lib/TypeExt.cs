using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;

namespace DotNetConsoleAppToolkit.Lib
{
    public static class TypeExt
    {
        public static bool InheritsFrom(this Type type,Type ancestorType)
        {
            while (type!=null)
            {
                if (type.BaseType == ancestorType) return true;
                type = type.BaseType;
            }
            return false;
        }

        public static bool HasInterface(this Type type, Type interfaceType)
            => type.GetInterface(interfaceType.FullName) != null;

        public static List<(string, object)> GetMemberValues(this object o)
        {
            var t = o.GetType();
            var r = new List<(string, object)>();
            foreach ( var f in t.GetFields() )
            {
                r.Add((f.Name, f.GetValue(o)));
            }
            foreach ( var p in t.GetProperties() )
            {
                r.Add((p.Name, p.GetValue(o)));
            }
            r.Sort(new Comparison<(string, object)>(
                (a, b) => a.Item1.CompareTo(b.Item1) )) ;
            return r;
        }
    }
}
