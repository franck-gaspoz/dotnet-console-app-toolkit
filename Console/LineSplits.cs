using System.Collections.Generic;

namespace DotNetConsoleAppToolkit.Console
{
    public class LineSplits
    {
        public readonly List<StringSegment> Splits;

        public readonly PrintSequences PrintSequences;

        public LineSplits(List<StringSegment> splits,PrintSequences printSequences)
        {
            Splits = splits;
            PrintSequences = printSequences;
        }

    }
}
