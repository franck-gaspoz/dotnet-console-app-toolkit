using DotNetConsoleAppToolkit.Lib.Data;
using System;
using System.Collections;
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
            // standard namespaces
            foreach (var ns in Enum.GetValues(typeof(VariableNameSpace)))
                _dataRegistry.Set(ns + "", new DataObject(ns+"",false));

            // Env vars
            var pfx = VariableNameSpace.Env + ".";
            foreach (DictionaryEntry envvar in Environment.GetEnvironmentVariables())
                _dataRegistry.Set(pfx+envvar.Key, envvar.Value);
        }

        public void Set(string path, object value)
            => _dataRegistry.Set(path, value);

        public void Unset(string path)
            => _dataRegistry.Unset(path);

        /// <summary>
        /// serch in data context the path according to these precedence rules:
        /// - full path
        /// - path related to Local
        /// - path related to Env
        /// - path related to Global
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public object Get(string path)
        {
            if (!_dataRegistry.Get(path,out var data))
                throw new VariableNotFoundException(GetVariableName(path));
            return data;
        }

        public T Get<T>(string path) => (T)Get(path);

        public IDataObject GetDataObject(string path) => (IDataObject)Get(path);

        public DataValue GetValue(string path)
        {
            if (!_dataRegistry.Get(path,out var data))
                throw new VariableNotFoundException(GetVariableName(path));
            return (DataValue)data;
        }

        public bool GetPathOwner(string path,out object data)
            => _dataRegistry.GetPathOwner(path,out data);

        public List<DataValue> GetDataValues() => _dataRegistry.GetDataValues();
    }
}
