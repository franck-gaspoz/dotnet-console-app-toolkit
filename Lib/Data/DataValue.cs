using System;
using System.Linq;

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

        public (bool found, object data) Get(ArraySegment<string> path)
            => Get(Value, path);

        (bool found, object data) Get(object target, ArraySegment<string> path)
        {
            if (target == null) return (false,null);
            if (path.Count == 0) return (false,null);
            var attrname = path[0];
            var fieldsInfos = target.GetType().GetFields().ToDictionary((x) => x.Name);
            if (fieldsInfos.TryGetValue(attrname, out var fieldInfo))
            {
                if (path.Count == 1) return (true,fieldInfo.GetValue(target));
                return Get(target, path.Slice(1));
            }
            else
                return (false,null);
        }

        public (bool found, object data) GetPathOwner(ArraySegment<string> path)
            => GetPathOwner(Value, path);

        (bool found, object data) GetPathOwner(object target, ArraySegment<string> path) { 
            if (path.Count == 0) return (false,null);
            var attrname = path[0];
            var fieldsInfos = target.GetType().GetFields().ToDictionary((x) => x.Name);
            if (fieldsInfos.TryGetValue(attrname, out var fieldInfo))
            {
                if (path.Count == 1) return (true,fieldInfo.GetValue(target));
                return GetPathOwner(target,path.Slice(1));
            }
            else
                return (false,null);
        }

        public bool Has(ArraySegment<string> path)
            => Has(Value, path);

        bool Has(object target, ArraySegment<string> path)
            => GetPathOwner(path).found;

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
