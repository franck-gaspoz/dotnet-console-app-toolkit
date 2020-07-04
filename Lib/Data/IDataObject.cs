using System;

namespace DotNetConsoleAppToolkit.Lib.Data
{
    public interface IDataObject
    {
        string Name { get; }
        DataObject Parent { get; }
        bool IsReadOnly { get; }

        object Get(ArraySegment<string> path);
        object GetPathOwner(ArraySegment<string> path);
        bool Has(ArraySegment<string> path);
        void Set(ArraySegment<string> path, object value);
        void Unset(ArraySegment<string> path);
    }
}