using System.Collections.Generic;
using System.Linq;
using static DotNetConsoleAppToolkit.Component.Data.VariableSyntax;

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
            var (f, _) = RootObject.Get(p);
            if (f) _objects.AddOrReplace(path,value);            
        }

        public void Unset(string path)
        {
            RootObject.Unset(SplitPath(path));
            if (_objects.ContainsKey(path))
                _objects.Remove(path);
        }

        public (bool found, object data) Get(string path)
        {
            if (_objects.TryGetValue(path, out var value))
                return (true,value);
            var r = RootObject.Get(SplitPath(path));
            _objects.AddOrReplace(path, r);
            return r;
        }

        public (bool found, object data) GetPathOwner(string path)
            => RootObject.GetPathOwner(SplitPath(path));
               
    }
}
