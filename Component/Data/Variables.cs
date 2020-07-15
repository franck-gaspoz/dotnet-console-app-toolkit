using DotNetConsoleAppToolkit.Lib.Data;
using System;
using System.Collections.Generic;
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

        /// <summary>
        /// creates a standard variable rush with known namespaces
        /// </summary>
        public Variables() {
            foreach (var ns in Enum.GetValues(typeof(VariableNameSpace)))
                _dataRegistry.Set(ns + "");
        }

        public void Set(string path, object value)
            => _dataRegistry.Set(path, value);

        public void Unset(string path)
            => _dataRegistry.Unset(path);

        public object Get(string path)
        {
            if (!_dataRegistry.Get(path,out var data))
                throw new VariableNotFoundException(GetVariableName(path));
            return data;
        }

        public DataValue GetValue(string path)
        {
            if (!_dataRegistry.Get(path,out var data))
                throw new VariableNotFoundException(GetVariableName(path));
            return (DataValue)data;
        }

        public bool GetPathOwner(string path,out object data)
            => _dataRegistry.GetPathOwner(path,out data);

        public List<DataValue> ToList() => _dataRegistry.ToList();
    }
}
