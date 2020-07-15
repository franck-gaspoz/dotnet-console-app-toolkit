using System;
using System.Collections.Generic;

namespace DotNetConsoleAppToolkit.Lib.Data
{
    public sealed class DataObjectReadOnlyException : Exception
    {
        public DataObjectReadOnlyException(IDataObject dataObject) : base(
            $"DataObject name='{dataObject}' is read only"
            ) { }
    }

    public sealed class DataObject : IDataObject
    {
        public string Name { get; private set; }

        public DataObject Parent { get; private set; }

        private readonly Dictionary<string, IDataObject> _attributes
            = new Dictionary<string, IDataObject>();

        public bool IsReadOnly { get; private set; }

        public bool HasAttributes => _attributes.Count > 0;

        public DataObject(string name, bool isReadOnly = false)
        {
            Name = name;
            IsReadOnly = isReadOnly;
        }

        public void Set(ArraySegment<string> path, object value)
        {
            if (IsReadOnly) throw new DataObjectReadOnlyException(this);
            if (path.Count == 0) return;
            var attrname = path[0];
            if (_attributes.TryGetValue(attrname, out var attr))
            {
                if (path.Count == 1)
                {
                    _attributes[attrname] = new DataValue(attrname, value);
                }
                else
                    attr.Set(path.Slice(1), value);
            }
            else
            {
                var node = new DataObject(attrname);
                _attributes[attrname] = node;
                node.Set(path.Slice(1), value);
            }
        }

        public void Unset(ArraySegment<string> path)
        {
            if (IsReadOnly) throw new DataObjectReadOnlyException(this);
            if (path.Count == 0) return;
            var attrname = path[0];
            if (_attributes.TryGetValue(attrname, out var attr))
            {
                if (path.Count == 1)
                {
                    _attributes.Remove(attrname);
                }
                else
                    attr.Unset(path.Slice(1));
            }
        }

        public bool Get(ArraySegment<string> path,out object data)
        {
            data = null;
            if (path.Count == 0) return false;
            var attrname = path[0];
            if (_attributes.TryGetValue(attrname, out var attr))
            {
                if (path.Count == 1) return true;
                if (attr.Get(path.Slice(1), out var sdata))
                {
                    data = sdata;
                    return true;
                }
            }
            return false;
        }

        public bool Has(ArraySegment<string> path,out object data)
            => GetPathOwner(path,out data);

        public bool GetPathOwner(ArraySegment<string> path,out object data)
        {
            data = null;
            if (path.Count == 0) return false;
            var attrname = path[0];
            if (_attributes.ContainsKey(attrname))
            {
                if (path.Count == 1)
                {
                    data = this;
                    return true;
                }
                return GetPathOwner(path.Slice(1),out data);
            }
            return false;
        }
    }

}
