using static DotNetConsoleSdk.DotNetConsoleSdk;

namespace DotNetConsoleSdk
{
    class Program
    {
        static void Main(string[] args)
        {
            SetArgs(args);
            if (HasArgs)
                Print(Arg(0));
            else
            {
                //RunSampleCLI("(f=yellow,exec=[[!--System.IO.Path.GetFileName(System.Environment.CurrentDirectory)--]]) > ");
                RunSampleCLI("(f=yellow) > ");
            }
        }
    }
}
