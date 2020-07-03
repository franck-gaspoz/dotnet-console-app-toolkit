using DotNetConsoleAppToolkit.Console;
using System;
using System.Collections.Generic;

namespace DotNetConsoleAppToolkit.Lib.Data
{
    public sealed class DataObjectReadOnlyException : Exception
    {
        public DataObjectReadOnlyException(DataObject dataObject) : base(
            $"dataobject name='{dataObject}' is read only"
            ) { }
    }

    public class DataObject
    {
        public string Name { get; private set; }

        public DataObject Parent { get; private set; }

        private readonly Dictionary<string, DataObject> _attributes
            = new Dictionary<string, DataObject>();

        public readonly bool IsReadOnly = false;

        public DataObject(string name, bool isReadOnly = false)
        {
            Name = name;
            IsReadOnly = isReadOnly;
        }

        public void Set(string path,string value)
        {
            if (IsReadOnly) throw new DataObjectReadOnlyException(this);
        }

        public void Unset(string path)
        {
            if (IsReadOnly) throw new DataObjectReadOnlyException(this);
        }

        public virtual object Get(ArraySegment<string> path)
        {
            if (path.Count == 0) return null;
            var attrname = path[0];
            if (_attributes.TryGetValue(attrname, out var value))
            {
                if (path.Count == 1) return value;
                return value.Get(path.Slice(1));
            }
            else
                return null;
        }

        public virtual bool Has(ArraySegment<string> path)
        {
            if (path.Count == 0) return false;
            var attrname = path[0];
            if (_attributes.ContainsKey(attrname))
            {
                if (path.Count == 1) return true;
                return Has(path.Slice(1));
            }
            else
                return false;
        }
    }
}
