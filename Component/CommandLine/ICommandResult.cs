namespace DotNetConsoleAppToolkit.Component.CommandLine
{
    public interface ICommandResult
    {
        object GetOuputData();
        int ReturnCode { get; }
    }
}