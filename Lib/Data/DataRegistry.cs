using System.Collections.Generic;

namespace DotNetConsoleAppToolkit.Lib.Data
{
    public class DataRegistry
    {
        readonly Dictionary<string, object> _objects
            = new Dictionary<string, object>();

        DataObject RootObject = new DataObject("root");

        public void Set(string path,object value)
        {
            var p = SplitPath(path);
            RootObject.Set(p, value);
            _objects.AddOrReplace(path,
                (IDataObject)RootObject.Get(p));            
        }

        public void Unset(string path)
        {
            RootObject.Unset(SplitPath(path));
            if (_objects.ContainsKey(path))
                _objects.Remove(path);
        }

        public object Get(string path)
        {
            if (_objects.TryGetValue(path, out var value))
                return value;
            var r = RootObject.Get(SplitPath(path));
            _objects.AddOrReplace(path, r);
            return r;
        }

        public DataValue GetValue(string path)
        {
            if (_objects.TryGetValue(path, out var value))
                return (DataValue)value;
            var r = RootObject.Get(SplitPath(path));
            _objects.AddOrReplace(path, r);
            return (DataValue)r;
        }

        public DataObject GetPathOwner(string path)
            => (DataObject)RootObject.GetPathOwner(SplitPath(path));

        string[] SplitPath(string path)
        {
            return path?.Split('.');
        }
    }
}
