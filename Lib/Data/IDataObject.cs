using System;

namespace DotNetConsoleAppToolkit.Lib.Data
{
    public interface IDataObject
    {
        string Name { get; }
        DataObject Parent { get; }
        bool IsReadOnly { get; }

        (bool found, object data) Get(ArraySegment<string> path);
        (bool found, object data) GetPathOwner(ArraySegment<string> path);
        bool Has(ArraySegment<string> path);
        void Set(ArraySegment<string> path, object value);
        void Unset(ArraySegment<string> path);
    }
}