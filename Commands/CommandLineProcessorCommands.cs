using DotNetConsoleAppToolkit.Commands.FileSystem;
using DotNetConsoleAppToolkit.Component.CommandLine.CommandModel;
using DotNetConsoleAppToolkit.Component.CommandLine.Parsing;
using DotNetConsoleAppToolkit.Console;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using static DotNetConsoleAppToolkit.DotNetConsole;
using static DotNetConsoleAppToolkit.Lib.Str;
using cons = DotNetConsoleAppToolkit.DotNetConsole;

namespace DotNetConsoleAppToolkit.Component.CommandLine.Commands
{
    [Commands("commands of the command line processor")]
    public class CommandLineProcessorCommands : ICommandsDeclaringType
    {
        [Command("print help about commands,commands types and modules")]
        public void Help(
            CommandEvaluationContext context,
            [Option("s", "set short view: decrase output details")] bool shortView,
            [Option("v", "set verbose view: increase output details")] bool verboseView,
            [Option("all","list all commands")] bool all,
            [Option("t","filter commands list by command declaring type. if t is * list types",true,true)] string type,
            [Option("m", "filter commands list by module name. if m is * list modules", true,true)] string module,
            [Parameter("output help for the command with name 'commandName'", true)] string commandName
            )
        {
            var hascn = !string.IsNullOrWhiteSpace(commandName);
            var list = !all && !hascn;
            var cmds = context.CommandLineProcessor.AllCommands.AsQueryable();
            if (hascn)
                cmds = cmds.Where(x => x.Name.Equals(commandName, CommandLineParser.SyntaxMatchingRule));

            if (cmds.Count() > 0)
            {
                if (!string.IsNullOrWhiteSpace(type))
                {
                    if (type!="*" && !context.CommandLineProcessor.CommandDeclaringShortTypesNames.Contains(type))
                    {
                        Errorln($"unknown command declaring type: '{type}'");
                        return;
                    }

                    shortView = !verboseView;

                    if (type!="*")
                        cmds = cmds.Where(x => x.DeclaringTypeShortName == type);
                    else
                    {
                        var typenames = context.CommandLineProcessor.CommandDeclaringTypesNames.ToList();
                        var typelst = typenames.Select(x => Type.GetType(x)).ToList();
                        typelst.Sort((x, y) => x.Name.CompareTo(y.Name));
                        
                        var sfx = "Commands";
                        string TypeName(Type type)
                        {
                            var s = shortView ? type.Name : type.FullName;
                            if (shortView && s.EndsWith(sfx))
                                s = s.Substring(0, s.Length - sfx.Length);
                            return s;
                        }
                        var maxtl = typelst.Select(x => TypeName(x).Length).Max();

                        foreach (var typ in typelst)
                        {
                            var cmdattr = typ.GetCustomAttribute<CommandsAttribute>();                            
                            context.Out.Println(Darkcyan + TypeName(typ).PadRight(maxtl) + Tab + DefaultForegroundCmd + cmdattr.Description);
                        }
                        return;
                    }
                }
                if (cmds.Count()>0 && !string.IsNullOrWhiteSpace(module))
                {
                    if (module != "*" && !context.CommandLineProcessor.Modules.Values.Select(x => x.Name).Contains(module))
                    {
                        Errorln($"unknown command module: '{module}'");
                        return;
                    }

                    shortView = !verboseView;

                    if (module!="*")
                        cmds = cmds.Where(x => x.ModuleName == module);
                    else
                    {
                        var mods = context.CommandLineProcessor.Modules;
                        var modnames = mods.Values.Select(x => x.Name).ToList();
                        modnames.Sort();
                        var maxml = modnames.Select(x => x.Length).Max();
                        foreach (var modname in modnames)
                            context.Out.Println(Darkcyan+modname.PadRight(maxml)+Tab+ DefaultForegroundCmd + mods[modname].Description);
                        return;
                    }
                }
                var ncmds = cmds.ToList();
                ncmds.Sort(new Comparison<CommandSpecification>((x, y) => x.Name.CompareTo(y.Name)));
                cmds = ncmds.AsQueryable();
                if (cmds.Count() > 0)
                {
                    var maxcmdlength = cmds.Select(x => x.Name.Length).Max() + 1;
                    var maxcmdtypelength = cmds.Select(x => x.DeclaringTypeShortName.Length).Max() + 1;
                    var maxmodlength = cmds.Select(x => Path.GetFileNameWithoutExtension(x.MethodInfo.DeclaringType.Assembly.Location).Length).Max() + 1;
                    int n = 0;

                    if (list) shortView = !verboseView;

                    foreach (var cmd in cmds)
                    {
                        if (!list && n > 0) context.Out.Println();
                        PrintCommandHelp(context,cmd, shortView, list, maxcmdlength, maxcmdtypelength, maxmodlength, !string.IsNullOrWhiteSpace(commandName));
                        n++;
                    }
                }
            }
            else
                Errorln($"Command not found: '{commandName}'");
        }

        [Command("list modules of commands if no option specified, else load or unload modules of commands")]
        [SuppressMessage("Style", "IDE0071:Simplifier l’interpolation", Justification = "<En attente>")]
        [SuppressMessage("Style", "IDE0071WithoutSuggestion:Simplifier l’interpolation", Justification = "<En attente>")]
        public void Module(
            CommandEvaluationContext context, 
            [Option("l", "load a module from the given path", true, true)] FilePath loadModulePath = null,
            [Option("u","unload the module having the given name ",true,true)] string unloadModuleName = null
            )
        {
            var f = ColorSettings.Default.ToString();
            if (loadModulePath==null && unloadModuleName==null)
            {
                var col1length = context.CommandLineProcessor.Modules.Values.Select(x => x.Name.Length).Max() + 1;
                foreach (var kvp in context.CommandLineProcessor.Modules)
                {
                    context.Out.Println($"{Darkcyan}{kvp.Value.Name.PadRight(col1length,' ')}{f}{kvp.Value.Description} [types count={Cyan}{kvp.Value.TypesCount}{f} commands count={Cyan}{kvp.Value.CommandsCount}{f}]");
                    context.Out.Println($"{"".PadRight(col1length, ' ')}{ColorSettings.Label}assembly:{ColorSettings.HalfDark}{kvp.Value.Assembly.FullName}");
                    context.Out.Println($"{"".PadRight(col1length, ' ')}{ColorSettings.Label}path:    {ColorSettings.HalfDark}{kvp.Value.Assembly.Location}");
                }
            }
            if (loadModulePath!=null)
            {
                if (loadModulePath.CheckExists())
                {
                    var a = Assembly.LoadFrom(loadModulePath.FileSystemInfo.FullName);
                    var (typesCount, commandsCount) = context.CommandLineProcessor.RegisterCommandsAssembly(context,a);
                    if (commandsCount == 0)
                        Errorln("no commands have been loaded");
                    else
                        context.Out.Println($"loaded {ColorSettings.Numeric}{Plur("command",commandsCount,f)} in {ColorSettings.Numeric}{Plur("type", typesCount, f)}");
                }
            }
            if (unloadModuleName!=null)
            {
                if (context.CommandLineProcessor.Modules.Values.Any(x => x.Name==unloadModuleName))
                {
                    var (typesCount, commandsCount) = context.CommandLineProcessor.UnregisterCommandsAssembly(context,unloadModuleName);
                    if (commandsCount == 0)
                        Errorln("no commands have been unloaded");
                    else
                        context.Out.Println($"unloaded {ColorSettings.Numeric}{Plur("command", commandsCount, f)} in {ColorSettings.Numeric}{Plur("type", typesCount, f)}");
                }
                else
                    Errorln($"commands module '{unloadModuleName}' not registered");
            }
        }

        void PrintCommandHelp(
            CommandEvaluationContext context,
            CommandSpecification com, bool shortView = false, bool list = false, int maxcnamelength=-1, int maxcmdtypelength=-1, int maxmodlength=-1, bool singleout=false)
        {
#pragma warning disable IDE0071 // Simplifier l’interpolation
#pragma warning disable IDE0071WithoutSuggestion // Simplifier l’interpolation
            if (maxcnamelength == -1) maxcnamelength = com.Name.Length + 1;
            if (maxcmdtypelength == -1) maxcmdtypelength = com.DeclaringTypeShortName.Length + 1;       
            var col = singleout? "": "".PadRight(maxcnamelength, ' ');
            var f = GetCmd(PrintDirectives.f + "", DefaultForeground.ToString().ToLower());
            if (list)
            {
                if (!shortView)
                    context.Out.Println($"{Darkcyan}{com.ModuleName.PadRight(maxmodlength, ' ')}   {com.DeclaringTypeShortName.PadRight(maxcmdtypelength, ' ')}{Tab}{ColorSettings.Highlight}{com.Name.PadRight(maxcnamelength, ' ')}{Tab}{f}{com.Description}{ColorSettings.Default}");
                else
                    context.Out.Println($"{ColorSettings.Highlight}{com.Name.PadRight(maxcnamelength, ' ')}{f}{Tab}{com.Description}{ColorSettings.Default}");
            }
            else
            {
                if (singleout)
                {
                    context.Out.Println(com.Description);
                    if (com.ParametersCount > 0) context.Out.Print($"{Br}{col}{ColorSettings.Label}syntax: {f}{com.ToColorizedString()}{(!shortView ? Br : "")}");
                    context.Out.Println(GetPrintableDocText(com.LongDescription, list, shortView, 0));
                }
                else
                {
                    context.Out.Println($"{com.Name.PadRight(maxcnamelength, ' ')}{com.Description}");
                    if (com.ParametersCount > 0) context.Out.Print($"{Br}{col}{ColorSettings.Label}syntax: {f}{com.ToColorizedString()}{(!shortView ? Br : "")}");
                    context.Out.Print(GetPrintableDocText(com.LongDescription, list, shortView, maxcnamelength));
                }
            }

            if (!list)
            {
                if (com.ParametersCount > 0)
                {
                    if (!shortView)
                    {
                        var mpl = com.ParametersSpecifications.Values.Select(x => x.Dump(false).Length).Max() + TabLength;
                        foreach (var p in com.ParametersSpecifications.Values)
                        {
                            var ptype = (!p.IsOption && p.HasValue) ? $"of type: {Darkyellow}{p.ParameterInfo.ParameterType.Name}{f}" : "";
                            var pdef = (p.HasValue && p.IsOptional && p.HasDefaultValue && p.DefaultValue!=null && (!p.IsOption || p.ParameterValueTypeName!=typeof(bool).Name )) ? ((ptype!=""?". ":"") + $"default value: {Darkyellow}{DumpAsText(p.DefaultValue)}{f}") : "";
                            var supdef = $"{ptype}{pdef}";
                            context.Out.Println($"{col}{Tab}{p.ToColorizedString(false)}{"".PadRight(mpl - p.Dump(false).Length, ' ')}{p.Description}");
                            if (!string.IsNullOrWhiteSpace(supdef)) context.Out.Println($"{col}{Tab}{" ".PadRight(mpl)}{supdef}");
                        }

                        if (string.IsNullOrWhiteSpace(com.Documentation)) context.Out.Println();
                        context.Out.Print(GetPrintableDocText(com.Documentation, list, shortView, singleout ? 0 : maxcnamelength));
                        
                    } else
                    {
                        context.Out.Println(GetPrintableDocText(com.Documentation, list, shortView, singleout ? 0 : maxcnamelength));
                    }
                }
                if (!shortView)
                {
                    context.Out.Println($"{col}{ColorSettings.Label}type  : {ColorSettings.DarkLabel}{com.DeclaringTypeShortName}");
                    context.Out.Println($"{col}{ColorSettings.Label}module: {ColorSettings.DarkLabel}{com.ModuleName}{ColorSettings.Default}");
                }
            }
#pragma warning restore IDE0071WithoutSuggestion // Simplifier l’interpolation
#pragma warning restore IDE0071 // Simplifier l’interpolation
        }

        string GetPrintableDocText(string docText,bool list,bool shortView,int leftMarginSize)
        {
            if (string.IsNullOrWhiteSpace(docText) || shortView || list) return "";
            var lineStart = Environment.NewLine;
            var prfx0 = "{]=);:_&é'(";
            var prfx1 = "$*^ùè-_à'";
            docText = docText.Replace(lineStart, prfx0+prfx1);
            var lst = docText.Split(prfx0).AsQueryable();
            if (string.IsNullOrWhiteSpace(lst.FirstOrDefault())) lst = lst.Skip(1);
            lst = lst.Select(x => "".PadRight(leftMarginSize, ' ') + x + Br);
            return Br+string.Join( "", lst).Replace(prfx1, "");
        }

        [Command("set the command line prompt")]
        public void Prompt(
            CommandEvaluationContext context, 
            [Parameter("text of the prompt", false)] string prompt
            )
        {
            context.CommandLineProcessor.AssertCommandLineProcessorHasACommandLineReader();
            context.CommandLineProcessor.CmdLineReader.SetPrompt(prompt);
        }

        [Command("exit the shell")]
        public void Exit(
            CommandEvaluationContext context
            )
        {
            cons.Exit();
        }

        [Command("print command processor infos")]
        public void Cpinfo(
            CommandEvaluationContext context
            )
        {
            context.CommandLineProcessor.PrintInfo(context);
        }

        [Command("displays the commands history list or manipulate it")]
        [SuppressMessage("Style", "IDE0071WithoutSuggestion:Simplifier l’interpolation", Justification = "<En attente>")]
        [SuppressMessage("Style", "IDE0071:Simplifier l’interpolation", Justification = "<En attente>")]
        public List<string> History(
            CommandEvaluationContext context, 
            [Option("i", "invoke the command at the entry number in the history list", true, true)] int num,
            [Option("c", "clear the loaded history list")] bool clear,
            [Option("w", "write history lines to the history file (content of the file is replaced)")]
            [OptionRequireParameter("file")]  bool writeToFile,
            [Option("a", "append history lines to the history file")]
            [OptionRequireParameter("file")]  bool appendToFile,
            [Option("r","read the history file and append the content to the history list")] 
            [OptionRequireParameter("file")]  bool readFromFile,
            [Option("n","read the history file and append the content not already in the history list to the history list")] 
            [OptionRequireParameter("file")] bool appendFromFile,
            [Parameter(1,"file",true)] FilePath file
            )
        {
            var hist = context.CommandLineProcessor.CmdsHistory.History;
            var max = hist.Count().ToString().Length;
            int i = 1;
            var f = DefaultForegroundCmd;

            if (num>0)
            {
                if (num<1 || num>hist.Count)
                {
                    Errorln($"history entry number out of range (1..{hist.Count})");
                    return context.CommandLineProcessor.CmdsHistory.History;
                }
                var h = hist[num-1];
                context.CommandLineProcessor.CmdLineReader.SendNextInput(h);
                return context.CommandLineProcessor.CmdsHistory.History;
            }

            if (clear)
            {
                context.CommandLineProcessor.CmdsHistory.ClearHistory();                
                return context.CommandLineProcessor.CmdsHistory.History;
            }

            if (appendToFile || readFromFile || appendFromFile || writeToFile)
            {
                file ??= context.CommandLineProcessor.CmdsHistory.UserCommandsHistoryFilePath;
                if (file.CheckPathExists())
                {
                    if (writeToFile)
                    {
                        File.Delete(context.CommandLineProcessor.CmdsHistory.UserCommandsHistoryFilePath.FullName);
                        File.AppendAllLines(file.FullName, hist);
                    }
                    if (appendToFile) File.AppendAllLines(file.FullName, hist);
                    if (readFromFile)
                    {
                        var lines = File.ReadAllLines(file.FullName);
                        foreach (var line in lines) context.CommandLineProcessor.CmdsHistory.HistoryAppend(line);
                        context.CommandLineProcessor.CmdsHistory.HistorySetIndex(-1,false);
                    }
                    if (appendFromFile)
                    {
                        var lines = File.ReadAllLines(file.FullName);
                        foreach (var line in lines) if (!context.CommandLineProcessor.CmdsHistory.HistoryContains(line)) context.CommandLineProcessor.CmdsHistory.HistoryAppend(line);
                        context.CommandLineProcessor.CmdsHistory.HistorySetIndex(-1,false);
                    }
                }
                return context.CommandLineProcessor.CmdsHistory.History;
            }

            foreach ( var h in hist )
            {
                if (context.CommandLineProcessor.CancellationTokenSource.IsCancellationRequested)
                    break; 
                var hp = $"  {ColorSettings.Numeric}{i.ToString().PadRight(max + 2, ' ')}{f}";
                context.Out.Print(hp);
                Out.ConsolePrint(h, true);
                i++;
            }
            return context.CommandLineProcessor.CmdsHistory.History;
        }

        [Command("repeat the previous command if there is one, else does nothing")]
        [CommandName("!!")]
        public string HistoryPreviousCommand(
            CommandEvaluationContext context
            )
        {
            var lastCmd = context.CommandLineProcessor.CmdsHistory.History.LastOrDefault();
            context.CommandLineProcessor.AssertCommandLineProcessorHasACommandLineReader();
            if (lastCmd != null) context.CommandLineProcessor.CmdLineReader.SendNextInput(lastCmd);
            return lastCmd;
        }

        [Command("repeat the command specified by absolute or relative line number in command history list")]
        [CommandName("!")]        
        public string HistoryPreviousCommand(
            CommandEvaluationContext context,
            [Parameter("line number in the command history list if positive, else current command minus n if negative (! -1 equivalent to !!)")] int n
            )
        {
            var h = context.CommandLineProcessor.CmdsHistory.History;
            string lastCmd = null;
            var index = (n < 0) ? h.Count + n : n-1;
            if (index < 0 || index >= h.Count)
                Errorln($"line number out of bounds of commands history list (1..{h.Count})");
            else
            {
                lastCmd = h[index];
                context.CommandLineProcessor.AssertCommandLineProcessorHasACommandLineReader();
                context.CommandLineProcessor.CmdLineReader.SendNextInput(lastCmd);
            }
            return lastCmd;
        }
    }
}
