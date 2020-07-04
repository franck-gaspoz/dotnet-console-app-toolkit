using DotNetConsoleAppToolkit.Lib.Data;
using System;
using static DotNetConsoleAppToolkit.Component.Data.VariableSyntax;

namespace DotNetConsoleAppToolkit.Component.Data
{
    /// <summary>
    /// variables data store
    /// </summary>
    public class Variables
    {
        public sealed class VariableNotFoundException : Exception
        {
            public VariableNotFoundException(string variableName)
                : base($"variable not found: '{variableName}'")
            { }
        }

        protected readonly DataRegistry _dataRegistry = new DataRegistry();

        public Variables() {
            
        }

        public void Set(string path, object value)
        {

        }

        public void Unset(string path)
        {

        }

        public object Get(string path)
        {
            var (f,r) = _dataRegistry.Get(path);
            if (!f) throw new VariableNotFoundException(GetVariableName(path));
            return r;
        }

        public DataValue GetValue(string path)
        {
            var (f, r) = _dataRegistry.GetValue(path);
            if (!f) throw new VariableNotFoundException(GetVariableName(path));
            return (DataValue)r;
        }

        public (bool found,object data) GetPathOwner(string path)
            => _dataRegistry.GetPathOwner(path);
    }
}
