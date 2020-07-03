using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace DotNetConsoleAppToolkit.Lib.Data
{
    public class DataRegistry
    {
        public readonly Dictionary<string, DataObject> Objects
            = new Dictionary<string, DataObject>();

        
    }
}
