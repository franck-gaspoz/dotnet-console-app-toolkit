using DotNetConsoleSdk.Component.CommandLine.CommandModel;

namespace DotNetConsoleSdk.Component.CommandLine.Parsing
{
    public class MatchingParameter<T> : IMatchingParameter
    {
        public CommandParameterSpecification CommandParameterSpecification { get; protected set; }
        public T Value { get; protected set; }

        public object GetValue() => Value;

        public void SetValue(object value)
        {
            Value = (T)value;
        }

        public MatchingParameter(CommandParameterSpecification commandParameterSpecification, T value) 
        {
            Value = value;
            CommandParameterSpecification = commandParameterSpecification;
        }

        public MatchingParameter(CommandParameterSpecification commandParameterSpecification)
        {
            CommandParameterSpecification = commandParameterSpecification;
        }

        public override string ToString()
        {
            return $"{CommandParameterSpecification.ToString()} = {Value}";
        }
    }
}
