using System.Collections.Generic;
using System.Text;

namespace DotNetConsoleAppToolkit.Console
{
    public class PrintSequences
    {
        public readonly List<PrintSequence> List
            = new List<PrintSequence>();

        public void Add(PrintSequence printSequence) => List.Add(printSequence);

        public override string ToString()
        {
            var r = new StringBuilder();
            foreach (var printSequence in List)
                r.AppendLine(printSequence.ToString());
            return r.ToString();
        }

        public string ToStringPattern()
        {
            var r = new StringBuilder();
            foreach (var printSequence in List)
                r.Append(printSequence.ToStringPattern());
            return r.ToString();
        }
    }
}
