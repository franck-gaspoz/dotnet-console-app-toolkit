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

        public List<DataValue> ToList()
        {
            var r = new List<DataValue>();
            var values = _objects.Values.Where(x => x is DataValue).Cast<DataValue>();
            r.AddRange(values);
            //var subRegistries = _objects.Values.Where(x => x is DataObject);*/
            return r;
        }

        public void Set(string path,object value=null)
        {
            var p = SplitPath(path);
            RootObject.Set(p, value);
            if (RootObject.Get(p,out var data) && !_objects.ContainsKey(path))
                _objects.AddOrReplace(path,value);            
        }

        public void Unset(string path)
        {
            RootObject.Unset(SplitPath(path));
            if (_objects.ContainsKey(path))
                _objects.Remove(path);
        }

        public bool Get(string path,out object data)
        {
            if (_objects.TryGetValue(path, out var value))
            {
                data = value;
                return true;
            }
            if (RootObject.Get(SplitPath(path),out var sdata))
            {
                _objects.AddOrReplace(path, sdata);
                data = sdata;
                return true;
            }
            data = null;
            return false;
        }

        public bool GetPathOwner(string path,out object data)
            => RootObject.GetPathOwner(SplitPath(path),out data);
               
    }
}
