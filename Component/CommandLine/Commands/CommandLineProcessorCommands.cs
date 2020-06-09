using DotNetConsoleSdk.Component.CommandLine.CommandModel;
using DotNetConsoleSdk.Component.CommandLine.Parsing;
using System;
using System.Linq;
using System.Reflection.Metadata;
using static DotNetConsoleSdk.DotNetConsole;
using static DotNetConsoleSdk.Lib.Str;
using cons = DotNetConsoleSdk.DotNetConsole;
using static DotNetConsoleSdk.Component.CommandLine.CommandLineProcessor;
using static DotNetConsoleSdk.Component.CommandLine.CommandLineReader.CommandLineReader;
using System.Diagnostics.CodeAnalysis;
using DotNetConsoleSdk.Component.CommandLine.Commands.FileSystem;
using System.Reflection;
using System.IO;

namespace DotNetConsoleSdk.Component.CommandLine.Commands
{
    [Commands("commands related to the command line processor (dnc shell)")]
    public class CommandLineProcessorCommands
    {
        [Command("print help about all commands or a specific command")]
        public void Help(
            [Option("s", "set short view", true)] bool shortView,
            [Option("l","list all commands names and their description")] bool list,
            [Option("t","filter commands list by command declaring type",true,true)] string type = "",
            [Option("m", "filter commands list by module name", true,true)] string module = "",
            [Parameter("prints help for this command name", true)] string commandName = ""
            )
        {
            var cmds = CommandLineProcessor.AllCommands.AsQueryable();
            if (!string.IsNullOrWhiteSpace(commandName))
                cmds = cmds.Where(x => x.Name.Equals(commandName, CommandLineParser.SyntaxMatchingRule));

            if (cmds.Count() > 0)
            {
                if (!string.IsNullOrWhiteSpace(type))
                {
                    if (!CommandLineProcessor.CommandDeclaringTypesNames.Contains(type))
                    {
                        Errorln($"unknown command declaring type: '{type}'");
                        return;
                    }
                    cmds = cmds.Where(x => x.DeclaringTypeShortName == type);
                }
                if (!string.IsNullOrWhiteSpace(module))
                {
                    if (!CommandLineProcessor.Modules.Values.Select(x => x.Name).Contains(module))
                    {
                        Errorln($"unknown command module: '{module}'");
                        return;
                    }
                    cmds = cmds.Where(x => Path.GetFileNameWithoutExtension(x.MethodInfo.DeclaringType.Assembly.Location) == module);
                }
                var ncmds = cmds.ToList();
                ncmds.Sort(new Comparison<CommandSpecification>((x, y) => x.Name.CompareTo(y.Name)));
                cmds = ncmds.AsQueryable();
                var maxcmdlength = cmds.Select(x => x.Name.Length).Max() + 1;
                var maxcmdtypelength = cmds.Select(x => x.DeclaringTypeShortName.Length).Max() + 1;
                var maxmodlength = cmds.Select(x => Path.GetFileNameWithoutExtension(x.MethodInfo.DeclaringType.Assembly.Location).Length).Max() + 1;
                int n = 0;
                foreach (var cmd in cmds)
                {
                    if (!list && n > 0) Println();
                    PrintCommandHelp(cmd, shortView ,list,maxcmdlength, maxcmdtypelength, maxmodlength,cmds.Count()==1);
                    n++;
                }
            }
            else
                Errorln($"Command not found: '{commandName}'");
        }

        [Command("list modules of commands if no option specified, either load or unload modules of commands")]
        [SuppressMessage("Style", "IDE0071:Simplifier l’interpolation", Justification = "<En attente>")]
        [SuppressMessage("Style", "IDE0071WithoutSuggestion:Simplifier l’interpolation", Justification = "<En attente>")]
        public void Module(
            [Option("l", "load a module at this path", true, true)] FilePath loadModulePath = null,
            [Option("u","unload the module having this name ",true,true)] string unloadModuleName = null
            )
        {
            var f = GetCmd(KeyWords.f + "", DefaultForeground.ToString().ToLower());
            if (loadModulePath==null && unloadModuleName==null)
            {
                var col1length = Modules.Values.Select(x => x.Name.Length).Max() + 1;
                foreach (var kvp in Modules)
                {
                    Println($"{kvp.Value.Name.PadRight(col1length,' ')}{kvp.Value.Description} [types count={Cyan}{kvp.Value.TypesCount}{f} commands count={Cyan}{kvp.Value.CommandsCount}{f}]");
                    Println($"{"".PadRight(col1length, ' ')}{Cyan}assembly:{Gray}{kvp.Value.Assembly.FullName}");
                    Println($"{"".PadRight(col1length, ' ')}{Cyan}path:    {Gray}{kvp.Value.Assembly.Location}");
                }
            }
            if (loadModulePath!=null)
            {
                if (loadModulePath.CheckExists())
                {
                    var a = Assembly.LoadFrom(loadModulePath.FileSystemInfo.FullName);
                    var (typesCount, commandsCount) = RegisterCommandsAssembly(a);
                    if (commandsCount == 0)
                        Errorln("no commands have been loaded");
                    else
                        Println($"loaded {Cyan}{Plur("command",commandsCount,f)} in {Cyan}{Plur("type", typesCount, f)}");
                }
            }
            if (unloadModuleName!=null)
            {
                if (Modules.Values.Any(x => x.Name==unloadModuleName))
                {
                    var (typesCount, commandsCount) = UnregisterCommandsAssembly(unloadModuleName);
                    if (commandsCount == 0)
                        Errorln("no commands have been unloaded");
                    else
                        Println($"unloaded {Cyan}{Plur("command", commandsCount, f)} in {Cyan}{Plur("type", typesCount, f)}");
                }
                else
                    Errorln($"commands module '{unloadModuleName}' not registered");
            }
        }

        static void PrintCommandHelp(CommandSpecification com, bool shortView = false, bool list = false, int maxcnamelength=-1, int maxcmdtypelength=-1, int maxmodlength=-1, bool singleout=false)
        {
#pragma warning disable IDE0071 // Simplifier l’interpolation
#pragma warning disable IDE0071WithoutSuggestion // Simplifier l’interpolation
            if (maxcnamelength == -1) maxcnamelength = com.Name.Length + 1;
            if (maxcmdtypelength == -1) maxcmdtypelength = com.DeclaringTypeShortName.Length + 1;       
            var col = singleout? "": "".PadRight(maxcnamelength, ' ');
            var f = GetCmd(KeyWords.f + "", DefaultForeground.ToString().ToLower());
            if (list)
                Println($"{com.Name.PadRight(maxcnamelength, ' ')}{Darkcyan}{com.DeclaringTypeShortName.PadRight(maxcmdtypelength, ' ')}{f}{com.Description}");
            else
            {
                if (singleout)
                    Println(com.Description);
                else
                    Println($"{com.Name.PadRight(maxcnamelength, ' ')}{com.Description}");
            }

            if (!list)
            {
                Println($"{col}{Cyan}type  : {Darkcyan}{com.DeclaringTypeShortName}");
                Println($"{col}{Cyan}module: {Darkcyan}{Path.GetFileNameWithoutExtension(com.MethodInfo.DeclaringType.Assembly.Location)}");
                if (com.ParametersCount > 0)
                {
                    Println($"{col}{Cyan}syntax: {f}{com.ToColorizedString()}");                    
                    if (!shortView)
                    {
                        var mpl = com.ParametersSpecifications.Values.Select(x => x.Dump(false).Length).Max() + TabLength;
                        Println();
                        foreach (var p in com.ParametersSpecifications.Values)
                        {
                            var ptype = (!p.IsOption && p.HasValue) ? $"of type: {Darkyellow}{p.ParameterInfo.ParameterType.Name}{f}" : "";
                            var pdef = (!p.IsOption && p.HasValue) ? ($". default value: {Darkyellow}{DumpAsText(p.DefaultValue)}{f}") : "";
                            var supdef = $"{ptype}{pdef}";
                            Println($"{col}{Tab}{p.ToColorizedString(false)}{"".PadRight(mpl - p.Dump(false).Length, ' ')}{p.Description}");
                            if (!string.IsNullOrWhiteSpace(supdef)) Println($"{col}{Tab}{" ".PadRight(mpl)}{supdef}");
                        }
                    }
                }
            }
#pragma warning restore IDE0071WithoutSuggestion // Simplifier l’interpolation
#pragma warning restore IDE0071 // Simplifier l’interpolation
        }

        [Command("set the command line prompt")]
        public void Prompt(
            [Parameter("text of the prompt",false)] string prompt
            ) => SetPrompt(prompt);

        [Command("exit the shell")]
        public void Exit()
        {
            cons.Exit();
        }
    }
}
