using DotNetConsoleSdk.Component.CommandLine.CommandModel;

namespace DotNetConsoleSdk.Component.CommandLine.Parsing
{
    public interface IMatchingParameter
    {
        CommandParameterSpecification CommandParameterSpecification { get; }
        object GetValue();
        void SetValue(object value);
    }
}
