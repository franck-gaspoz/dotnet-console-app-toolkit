using static DotNetConsoleSdk.Component.CLI.CLI;
using DotNetConsoleSdk.Component.Shell;
using static DotNetConsoleSdk.DotNetConsole;

namespace DotNetConsoleSdk
{
    class Program
    {
        static void Main(string[] args)
        {
            InitializeCLI(args);
            if (HasArgs)
                Print(Arg(0));
            else
            {
                //RunSampleCLI("(f=yellow,exec=[[System.IO.Path.GetFileName(System.Environment.CurrentDirectory)]]) > ");
                var returnCode = ShellSample.RunShell(args,"(f=yellow)> ");
            }
        }
    }
}
