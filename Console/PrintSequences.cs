using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace DotNetConsoleAppToolkit.Console
{
    public class PrintSequences : IEnumerable<PrintSequence>
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

        public IEnumerator<PrintSequence> GetEnumerator()
        {
            return List.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return List.GetEnumerator();
        }

        public int TextLength
        {
            get
            {
                int n = 0;
                foreach (var seq in List)
                    if (seq.IsText) n += seq.Length;
                return n;
            }
        }
    }
}
