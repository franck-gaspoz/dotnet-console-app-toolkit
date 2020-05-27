using DotNetConsoleSdk.Component.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static DotNetConsoleSdk.Component.Shell.ShellSdk;
using static DotNetConsoleSdk.DotNetConsoleSdk;
using sc = System.Console;

namespace DotNetConsoleSdk.Component.Shell
{
    public static class ShellSdkSample
    {
        static void InitUI()
        {
            EchoOn(Path.Combine(TempPath, "trace.txt"));

            Clear();
            SetWorkArea(0, 4, -1, -3);

            AddFrame((frame) =>
            {
                var s = "".PadLeft(frame.ActualWidth, '-');
                var t = " dotnet-console-sdk - shell sdk sample";
                return new List<string> {
                        $"{Bdarkblue}{Cyan}{s}",
#pragma warning disable IDE0071
#pragma warning disable IDE0071WithoutSuggestion
                        $"{Bdarkblue}{Cyan}|{t}{White}{"".PadLeft(Math.Max(0, frame.ActualWidth - 2 - t.Length))}{Cyan}|",
#pragma warning restore IDE0071WithoutSuggestion
#pragma warning restore IDE0071
                        $"{Bdarkblue}{Cyan}{s}"
                    };
            }, ConsoleColor.DarkBlue, 0, 0, -1, 3, DrawStrategy.OnViewResizedOnly, false);

            string GetCurrentDriveInfo()
            {
                var rootDirectory = Path.GetPathRoot(Environment.CurrentDirectory);
                var di = DriveInfo.GetDrives().Where(x => x.RootDirectory.FullName == rootDirectory).FirstOrDefault();
                return (di == null) ? "?" : $"{rootDirectory} {HumanFormatOfSize(di.AvailableFreeSpace, 0, "")}/{HumanFormatOfSize(di.TotalSize, 0, "")} ({di.DriveFormat})";
            }

            AddFrame((frame) =>
            {
                return new List<string> {
                        $"{Bdarkblue} {Green}cur: {Cyan}{CursorLeft},{CursorTop}{White}"
                        +$" | {Green}win: {Cyan}{sc.WindowLeft},{sc.WindowTop}"
                        +$",{sc.WindowWidth},{sc.WindowHeight}{White}"
                        +$" | {(sc.CapsLock?$"{Cyan}Caps":$"{Darkgray}Caps")}"
                        +$" {(sc.NumberLock?$"{Cyan}Num":$"{Darkgray}Num")}{White}"
                        +$" | {Green}in={Cyan}{sc.InputEncoding.CodePage}"
                        +$" {Green}out={Cyan}{sc.OutputEncoding.CodePage}{White}"
                        +$" | {Green}drive: {Cyan}{GetCurrentDriveInfo()}{White}"
                        +$" | {Cyan}{System.DateTime.Now}{White}      "
                    };
            }, ConsoleColor.DarkBlue, 0, -1, -1, 1, DrawStrategy.OnViewResizedOnly, false, 1000);

            SetCursorAtBeginWorkArea();
        }

        public static void RunShell(string prompt = null)
        {
            try
            {
                ShellSdk.Initialize();
                InitUI();
                ConsolePrint("AAAAAAAAAAAAAAAAAAAAAAAAAAAAABBBBBBBBBBBBBBBBBBBBBBBBBBBBBCCCCC",true);
                //ConsolePrint("BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBCCCCC",true);
                BeginReadln(prompt);
            }
            catch (Exception initException)
            {
                LogError(initException);
                Exit();
            }
        }
    }
}
