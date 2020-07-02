   
namespace DotNetConsoleAppToolkit.Component.CommandLine
{
    public class CommandResult<T> : ICommandResult
    {
        public CommandEvaluationContext CommandEvaluationContext { get; protected set; }
        public T OutputData;
        public int ReturnCode { get; protected set; }

        public CommandResult(
            CommandEvaluationContext commandEvaluationContext,
            T outputData
            )
        {
            OutputData = outputData;
            CommandEvaluationContext = commandEvaluationContext;
            ReturnCode = (int)CommandLine.ReturnCode.OK;
        }

        public CommandResult(
            CommandEvaluationContext commandEvaluationContext,
            int returnCode
            )
        {
            CommandEvaluationContext = commandEvaluationContext;
            ReturnCode = returnCode;
        }

        public CommandResult(
            CommandEvaluationContext commandEvaluationContext
            )
        {
            CommandEvaluationContext = commandEvaluationContext;
            ReturnCode = (int)CommandLine.ReturnCode.OK;
        }

        public object GetOuputData() => OutputData;
    }
}
