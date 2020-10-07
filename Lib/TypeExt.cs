using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Reflection;

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

        public static List<(string name, object value, MemberInfo memberInfo)> GetMemberValues(this object o)
        {
            var t = o.GetType();
            var r = new List<(string, object,MemberInfo)>();
            foreach ( var f in t.GetFields() )
            {
                r.Add((f.Name, f.GetValue(o),f));
            }
            foreach ( var p in t.GetProperties() )
            {
                if (p.GetGetMethod().GetParameters().Length==0)
                    r.Add((p.Name, p.GetValue(o),p));
                else
                    // indexed property
                    r.Add((p.Name, "indexed property" , p));
            }
            r.Sort(new Comparison<(string, object,MemberInfo)>(
                (a, b) => a.Item1.CompareTo(b.Item1) )) ;
            return r;
        }

        public static Type GetMemberValueType(this MemberInfo memberInfo)
        {
            if (memberInfo is FieldInfo field) return field.FieldType;
            if (memberInfo is PropertyInfo prop) return prop.PropertyType;
            return null;
        }
    }
}
