using DotNetConsoleAppToolkit.Component.CommandLine.CommandModel;
using static DotNetConsoleAppToolkit.DotNetConsole;

namespace DotNetConsoleAppToolkit.Component.CommandLine.Commands
{
    [Commands("commands of the console")]
    public class ConsoleCommands : CommandsType
    {
        public ConsoleCommands(CommandLineProcessor commandLineProcessor) : base(commandLineProcessor) { }

        const string _printDocText =
@"text can contains (uon)print directives(tdoff) that changes the print behavior. 
the print directive syntax is formed according to these pattern:

(f=darkyellow)(printDirective) or (printDirective=printDirectiveValue)(rdc)

- multiple print directives can separated by a (f=darkyellow),(rdc) to be grouped in a single text in parentheses: (f=darkyellow)(printDirective1,printDirective2=..,printDirective3)(rdc)
- a print directiveValue can be written inside a 'code' text block, depending on each print directive, with the syntax: (f=darkyellow)[[...]](rdc)
- symbols of this grammar can be configured throught the class (uon)DotNetConsole(tdoff)

current print directives are:

    (f=yellow)f=(f=darkyellow)ConsoleColor(rdc)      : set foreground color
    (f=yellow)b=(f=darkyellow)ConsoleColor(rdc)      : set background color
";

        [Command("write text to the output stream",null,
            _printDocText
            )]
        public void Print(
            [Parameter("text to be writen to output",true)] string expr = ""
            ) => DotNetConsole.Print(expr);

        [Command("write text to the output stream followed by a line break")]
        public void Println(
            [Parameter("text to be writen to output", true)] string expr = ""
            ) => DotNetConsole.Println(expr);

        [Command("clear console screen")]
        public void Cls() => Clear();

        [Command("hide cursor")]
        public void HideCursor() => HideCur();

        [Command("show cursor")]
        public void ShowCursor() => ShowCur();
    }
}
