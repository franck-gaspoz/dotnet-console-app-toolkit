using DotNetConsoleAppToolkit.Component.Data;
using DotNetConsoleAppToolkit.Console;
using System.IO;

namespace DotNetConsoleAppToolkit.Component.CommandLine
{
    public class CommandEvaluationContext
    {
        public readonly CommandLineProcessor CommandLineProcessor;
        public readonly ConsoleTextWriterWrapper Out;
        public readonly TextWriterWrapper Err;
        public readonly TextReader In;
        public readonly object InputData;
        public readonly Variables Variables;

        public CommandEvaluationContext(
            CommandLineProcessor commandLineProcessor, 
            ConsoleTextWriterWrapper @out, 
            TextReader @in, 
            TextWriterWrapper err, 
            object inputData)
        {
            CommandLineProcessor = commandLineProcessor;
            Out = @out;
            In = @in;
            Err = err;
            InputData = inputData;
            Variables = new Variables();
        }
    }
}
