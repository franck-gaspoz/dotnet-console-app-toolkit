using System;
using System.Linq;
using System.Linq.Expressions;

namespace DotNetConsoleAppToolkit.Lib.Data
{
    public sealed class DataValueReadOnlyException : Exception
    {
        public DataValueReadOnlyException(IDataObject dataObject) : base(
            $"DataValue name='{dataObject}' is read only"
            )
        { }
    }

    public sealed class DataValue : IDataObject
    {
        public string Name { get; private set; }
        public DataObject Parent { get; private set; }

        public object Value { get; private set; }
        public Type ValueType { get; private set; }
        public bool HasValue { get; private set; }

        public bool IsReadOnly { get; private set; }
        public bool HasAttributes => false;

        public DataValue(
            string name,
            object value,
            Type valueType = null,
            bool isReadOnly = false)
        {
            Name = name ?? throw new ArgumentNullException(nameof(Name));
            ValueType = valueType ?? value?.GetType();
            ValueType = ValueType ?? throw new ArgumentNullException(nameof(ValueType));
            IsReadOnly = isReadOnly;
        }

        public bool Get(ArraySegment<string> path,out object data)
            => Get(Value, path,out data);

        bool Get(object target, ArraySegment<string> path,out object data)
        {
            data = null;
            if (target == null) return false;
            if (path.Count == 0) return false;
            var attrname = path[0];
            var fieldsInfos = target.GetType().GetFields().ToDictionary((x) => x.Name);
            if (fieldsInfos.TryGetValue(attrname, out var fieldInfo))
            {
                if (path.Count == 1)
                {
                    data = fieldInfo.GetValue(target);
                    return true;
                }
                return Get(target, path.Slice(1),out data);
            }
            return false;
        }

        public bool GetPathOwner(ArraySegment<string> path,out object data)
            => GetPathOwner(Value, path,out data);

        bool GetPathOwner(object target, ArraySegment<string> path,out object data) {
            data = null;
            if (path.Count == 0) return false;
            var attrname = path[0];
            var fieldsInfos = target.GetType().GetFields().ToDictionary((x) => x.Name);
            if (fieldsInfos.TryGetValue(attrname, out var fieldInfo))
            {
                if (path.Count == 1)
                {
                    data = fieldInfo.GetValue(target);
                    return true;
                }
                return GetPathOwner(target,path.Slice(1),out data);
            }
            return false;
        }

        public bool Has(ArraySegment<string> path,out object data)
            => Has(Value, path,out data);

        bool Has(object target, ArraySegment<string> path, out object data)
            => GetPathOwner(target, path, out data);

        public void Set(ArraySegment<string> path, object value)
            => Set(this, path, value);

        void Set(object target, ArraySegment<string> path, object value)
        {
            if (IsReadOnly) throw new DataObjectReadOnlyException(this);
            if (target == null) return;
            if (path.Count == 0) return;
            var attrname = path[0];
            var fieldsInfos = target.GetType().GetFields().ToDictionary((x) => x.Name);
            if (fieldsInfos.TryGetValue(attrname, out var fieldInfo))
            {
                if (path.Count == 1)
                {
                    fieldInfo.SetValue(target, value);
                }
                else
                    Set(target, path.Slice(1), value);
            }
            else
                throw new DataValueReadOnlyException(this);
        }

        public void Unset(ArraySegment<string> path)
            => Unset(this, path);

        void Unset(object target, ArraySegment<string> path)
        {
            throw new DataValueReadOnlyException(this);
        }
    }
}
