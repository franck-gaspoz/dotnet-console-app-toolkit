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

- multiple print directives can be separated by a (f=darkyellow),(rdc) to be grouped in a single text in parentheses: (f=darkyellow)(printDirective1,printDirective2=..,printDirective3)(rdc)
- a print directive value can be written inside a 'code' text block, depending on each print directive, with the syntax: (f=darkyellow)[[...]](rdc)
- symbols of this grammar can be configured throught the class (uon)DotNetConsole(tdoff)

current print directives are:

    (1) (uon)colorization:(tdoff)

    (f=yellow)f=(f=darkyellow)ConsoleColor(rdc)      : set foreground color
    (f=yellow)f8=(f=darkyellow)Int32(rdc)            : set foreground 8bit color index, where 0 <= index <= 255 
    (f=yellow)f24=(f=darkyellow)Int32:Int32:Int32(rdc) : set foreground 24bit color red:green:blue, where 0 <= red,green,blue <= 255 
    (f=yellow)f=(f=darkyellow)ConsoleColor(rdc)      : set foreground color
    (f=yellow)b=(f=darkyellow)ConsoleColor(rdc)      : set background color
    (f=yellow)b8=(f=darkyellow)Int32(rdc)            : set background 8bit color index, where 0 <= index <= 255
    (f=yellow)b24=(f=darkyellow)Int32:Int32:Int32(rdc) : set background 24bit color red:green:blue, where 0 <= red,green,blue <= 255 
    (f=yellow)df=(f=darkyellow)ConsoleColor(rdc)     : set default foreground
    (f=yellow)db=(f=darkyellow)ConsoleColor(rdc)     : set default background
    (f=yellow)bkf(rdc)                 : backup foreground color
    (f=yellow)bkb(rdc)                 : backup background color
    (f=yellow)rsf(rdc)                 : restore foreground color
    (f=yellow)rsb(rdc)                 : restore background color
    (f=yellow)rdc(rdc)                 : restore default colors
    
    (2) (uon)text decoration (vt100):(tdoff)

    (f=yellow)uon(rdc)                 : underline on
    (f=yellow)invon(rdc)               : inverted colors on
    (f=yellow)tdoff(rdc)               : text decoration off and reset default colors

    (3) (uon)print flow control:(tdoff)

    (f=yellow)cls(rdc)                 : clear screen
    (f=yellow)br(rdc)                  : jump begin of next line (line break)   
    (f=yellow)bkcr(rdc)                : backup cursor position
    (f=yellow)rscr(rdc)                : restore cursor position
    (f=yellow)crx=(f=darkyellow)Int32(rdc)           : set cursor x ((f=cyan)0<=x<=WindowWidth(rdc))
    (f=yellow)cry=(f=darkyellow)Int32(rdc)           : set cursor y ((f=cyan)0<=y<=WindowHeight(rdc))
    (f=yellow)cleft(rdc)               : move cursor left
    (f=yellow)cright(rdc)              : move cursor right
    (f=yellow)cup(rdc)                 : move cursor up
    (f=yellow)cdown(rdc)               : move cursor down
    (f=yellow)cnleft=(f=darkyellow)Int32(rdc)        : move cursor n characters left
    (f=yellow)cnright=(f=darkyellow)Int32(rdc)       : move cursor n characters right
    (f=yellow)cnup=(f=darkyellow)Int32(rdc)          : move cursor n lines up
    (f=yellow)cndown=(f=darkyellow)Int32(rdc)        : move cursor n lines down
    (f=yellow)cl(rdc)                  : clear line
    (f=yellow)clleft(rdc)              : clear line from cursor left
    (f=yellow)clright(rdc)             : clear line from cursor right
    (f=yellow)chome(rdc)               : move cursor to upper left corner

    (4) (uon)script engine:(tdoff)

    (f=yellow)exec=(f=darkyellow)CodeBlock|[[CodeBlock]](rdc) : executes and print result of a C# code block

    (5) (uon)application control:(tdoff)

    (f=yellow)exit(rdc)                : exit the current process

    (f=darkyellow)ConsoleColor := darkblue|darkgreen|darkcyan|darkred|darkmagenta|darkyellow|gray|darkgray|blue|green|cyan|red|magenta|yellow|white(rdc) (not case sensitive)
";

        [Command("write text to the output stream",null,_printDocText
            )]
        public void Print(
            [Parameter("text to be writen to output",true)] string expr = ""
            ) => Out.Print(expr);

        [Command("write text to the output stream followed by a line break",null, _printDocText)]
        public void Println(
            [Parameter("text to be writen to output", true)] string expr = ""
            ) => Out.Println(expr);

        [Command("clear console screen")]
        public void Cls() => Out.ClearScreen();

        [Command("hide cursor")]
        public void HideCursor() => Out.HideCur();

        [Command("show cursor")]
        public void ShowCursor() => Out.ShowCur();
    }
}
