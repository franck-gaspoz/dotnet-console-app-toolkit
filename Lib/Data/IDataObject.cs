using System;

namespace DotNetConsoleAppToolkit.Lib.Data
{
    public interface IDataObject
    {
        string Name { get; }
        DataObject Parent { get; }
        bool IsReadOnly { get; }
        bool HasAttributes { get; }

        bool Get(ArraySegment<string> path,out object data );
        bool GetPathOwner(ArraySegment<string> path,out object data);
        bool Has(ArraySegment<string> path,out object data);
        void Set(ArraySegment<string> path, object value);
        void Unset(ArraySegment<string> path);
    }
}