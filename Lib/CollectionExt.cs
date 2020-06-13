using System.Collections.Generic;

namespace DotNetConsoleAppToolkit.Lib
{
    public static class CollectionExt
    {
        public static void Merge<T>(this List<T> mergeInto,List<T> merged)
        {
            foreach (var o in merged)
                if (!mergeInto.Contains(o))
                    mergeInto.Add(o);
        } 
    }
}
