using DotNetConsoleAppToolkit.Component.CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DotNetConsoleAppToolkit.Lib.Data
{
    public class DataValue : DataObject
    {
        public object Value;
        public Type ValueType { get; protected set; }
        public bool HasValue { get; protected set; }

        public DataValue(
            string name,
            object value,
            Type valueType = null) : base(name)
        {
            ValueType = valueType ?? value?.GetType();
        }

        static Dictionary<string,FieldInfo> _bkFieldsInfos;
        private Dictionary<string, FieldInfo> _fieldsInfos {
            get {
                if (_bkFieldsInfos == null) _bkFieldsInfos = 
                        this.GetType().GetFields()
                        .ToDictionary((x) => x.Name);
                return _bkFieldsInfos;
            }
        }

        public override object Get(ArraySegment<string> path)
        {
            return Get(Value, path);
        }

        object Get(object target, ArraySegment<string> path)
        {
            if (target == null) return null;
            if (path.Count == 0) return null;
            var attrname = path[0];
            var fieldsInfos = target.GetType().GetFields().ToDictionary((x) => x.Name);
            if (fieldsInfos.TryGetValue(attrname, out var fieldInfo))
            {
                if (path.Count == 1) return fieldInfo.GetValue(Value);
                return Get(Value,path.Slice(1));
            }
            else
                return null;
        }
    }
}
