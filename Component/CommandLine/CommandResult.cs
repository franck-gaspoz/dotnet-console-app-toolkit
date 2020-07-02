namespace DotNetConsoleAppToolkit.Component.CommandLine
{
    public class CommandResult<T>
    {
        public readonly CommandEvaluationContext CommandEvaluationContext;
        public T OutputData;
        public ReturnCode ReturnCode;

        public CommandResult(
            CommandEvaluationContext commandEvaluationContext,
            T outputData,
            ReturnCode returnCode
            )
        {
            OutputData = outputData;
            CommandEvaluationContext = commandEvaluationContext;
            ReturnCode = returnCode;
        }
    }
}
