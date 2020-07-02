   
namespace DotNetConsoleAppToolkit.Component.CommandLine
{
    public class CommandVoidResult : ICommandResult
    {
        public int ReturnCode { get; protected set; }

        public CommandEvaluationContext CommandEvaluationContext { get; protected set; }

        public object GetOuputData()
        {
            return null;
        }

        public CommandVoidResult(CommandEvaluationContext commandEvaluationContext)
        {
            CommandEvaluationContext = commandEvaluationContext;
            ReturnCode = (int)CommandLine.ReturnCode.OK;
        }

        public CommandVoidResult(
            CommandEvaluationContext commandEvaluationContext,
            int returnCode)
        {
            CommandEvaluationContext = commandEvaluationContext;
            ReturnCode = returnCode;
        }

        public CommandVoidResult(
            CommandEvaluationContext commandEvaluationContext,
            ReturnCode returnCode)
        {
            CommandEvaluationContext = commandEvaluationContext;
            ReturnCode = (int)returnCode;
        }
    }

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
            T outputData,
            int returnCode
            )
        {
            OutputData = outputData;
            CommandEvaluationContext = commandEvaluationContext;
            ReturnCode = returnCode;
        }

        public CommandResult(
            CommandEvaluationContext commandEvaluationContext,
            T outputData,
            ReturnCode returnCode
            )
        {
            OutputData = outputData;
            CommandEvaluationContext = commandEvaluationContext;
            ReturnCode = (int)returnCode;
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
            CommandEvaluationContext commandEvaluationContext,
            ReturnCode returnCode
            )
        {
            CommandEvaluationContext = commandEvaluationContext;
            ReturnCode = (int)returnCode;
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
