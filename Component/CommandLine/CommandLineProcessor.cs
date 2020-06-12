//#define enable_test_commands

using DotNetConsoleSdk.Component.CommandLine.CommandModel;
using DotNetConsoleSdk.Component.CommandLine.Parsing;
using DotNetConsoleSdk.Console;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using static DotNetConsoleSdk.Component.CommandLine.Parsing.CommandLineParser;
using static DotNetConsoleSdk.DotNetConsole;
using cmdlr = DotNetConsoleSdk.Component.CommandLine.CommandLineReader;
using cons = System.Console;

namespace DotNetConsoleSdk.Component.CommandLine
{
    public static class CommandLineProcessor
    {
        public const string AppName = "dnsh";
        public const string AppLongName = "Dot Net Shell";
        public const string AppEditor = "released on June 2020 under licence MIT";

        #region attributes

        public static CancellationTokenSource CancellationTokenSource;

        public static int ReturnCodeOK = 0;
        public static int ReturnCodeError = 1;
        public static int ReturnCodeNotDefined = 1;

        static string[] _args;
        static bool _isInitialized = false;

        static readonly Dictionary<string, List<CommandSpecification>> _commands = new Dictionary<string, List<CommandSpecification>>();

        public static readonly ReadOnlyDictionary<string, List<CommandSpecification>> Commands = new ReadOnlyDictionary<string, List<CommandSpecification>>(_commands);

        public static List<CommandSpecification> AllCommands {
            get {
                var coms = new List<CommandSpecification>();
                foreach (var kvp in _commands)
                    foreach (var com in kvp.Value)
                        coms.Add(com);
                coms.Sort(new Comparison<CommandSpecification>((x, y) => x.Name.CompareTo(y.Name)));
                return coms;
            }
        }

        static readonly SyntaxAnalyser _syntaxAnalyzer = new SyntaxAnalyser();

        static readonly Dictionary<string, CommandsModule> _modules = new Dictionary<string, CommandsModule>();

        public static IReadOnlyDictionary<string, CommandsModule> Modules => new ReadOnlyDictionary<string, CommandsModule>(_modules);

        public static IEnumerable<string> CommandDeclaringTypesNames => AllCommands.Select(x => x.DeclaringTypeShortName);

        public static CommandsHistory CmdsHistory;

        public static cmdlr.CommandLineReader CmdLineReader;

        #endregion

        #region cli methods

        public static string Arg(int n)
        {
            if (_args == null) return null;
            if (_args.Length <= n) return null;
            return _args[n];
        }

        public static bool HasArgs => _args != null && _args.Length > 0;

        static void SetArgs(string[] args)
        {
            _args = (string[])args?.Clone();
        }

        #endregion

        #region command engine operations

        public static void InitializeCommandProcessor(string[] args, cmdlr.CommandLineReader commandLineReader,bool printInfo=true)
        {
            SetArgs(args);
            if (!_isInitialized)
            {
                _isInitialized = true;

                cons.ForegroundColor = DefaultForeground;
                cons.BackgroundColor = DefaultBackground;

                CmdsHistory = new CommandsHistory(UserProfileFolder);
                CmdLineReader = commandLineReader;

                RegisterCommandsAssembly(Assembly.GetExecutingAssembly());
#if enable_test_commands
                RegisterCommandsClass<TestCommands>();
#endif
                if (printInfo)
                {
                    var f = DefaultForegroundCmd;
                    Println($" {ColorSettings.Label}{AppLongName} ({AppName}) version {Assembly.GetExecutingAssembly().GetName().Version}");
                    Println($" {AppEditor}");
                    Println($" OS {Environment.OSVersion} CLR {Environment.Version}");
                    Println();
                }
            }
        }

        public static (int typesCount,int commandsCount) UnregisterCommandsAssembly(string assemblyName)
        {
            var module = _modules.Values.Where(x => x.Name == assemblyName).FirstOrDefault();
            if (module!=null)
            {
                foreach (var com in AllCommands)
                    if (com.MethodInfo.DeclaringType.Assembly == module.Assembly)
                        RemoveCommand(com);
                return (module.TypesCount, module.CommandsCount);
            }
            else
            {
                Errorln($"commands module '{assemblyName}' not registered");
                return (0, 0);
            }
        }

        static bool RemoveCommand(CommandSpecification comSpec)
        {
            if (_commands.TryGetValue(comSpec.Name, out var cmdLst))
            {
                var r = cmdLst.Remove(comSpec);
                if (r)
                    _syntaxAnalyzer.Remove(comSpec);
                if (cmdLst.Count == 0)
                    _commands.Remove(comSpec.Name);
                return r;
            }
            return false;
        }

        public static (int typesCount, int commandsCount) RegisterCommandsAssembly(string assemblyPath)
        {
            var assembly = Assembly.LoadFrom(assemblyPath);
            return RegisterCommandsAssembly(assembly);
        }

        public static (int typesCount,int commandsCount) RegisterCommandsAssembly(Assembly assembly)
        {
            if (_modules.ContainsKey(assembly.FullName))
            {
                Errorln($"commands module already registered: '{assembly.FullName}'");
                return (0,0);
            }
            var typesCount = 0;
            var comTotCount = 0;
            foreach ( var type in assembly.GetTypes())
            {
                var comsAttr = type.GetCustomAttribute<CommandsAttribute>();

                var comCount = 0;
                if (comsAttr != null)
                    comCount = RegisterCommandsClass(type,false);                
                if (comCount > 0)
                    typesCount++;
                comTotCount += comCount;
            }
            if (typesCount > 0)
            {
                var descAttr = assembly.GetCustomAttribute<AssemblyDescriptionAttribute>();
                var description = (descAttr != null) ? descAttr.Description : "";
                _modules.Add(assembly.FullName, new CommandsModule(Path.GetFileNameWithoutExtension(assembly.Location), description, assembly, typesCount, comTotCount));
            }
            return (typesCount,comTotCount);    
        }

        public static void RegisterCommandsClass<T>() => RegisterCommandsClass(typeof(T),true);

        public static int RegisterCommandsClass(Type type) => RegisterCommandsClass(type, true);

        static int RegisterCommandsClass(Type type,bool registerAsModule)
        {
            var comsCount = 0;
            object instance = Activator.CreateInstance(type);            
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            if (registerAsModule && _modules.ContainsKey(type.FullName))
            {
                Errorln($"commands type '{type.FullName}' already registered");
                return 0;
            }
            foreach ( var method in methods )
            {
                var cmd = method.GetCustomAttribute<CommandAttribute>();
                if (cmd!=null)
                {
                    var paramspecs = new List<CommandParameterSpecification>();
                    bool syntaxError = false;
                    foreach ( var parameter in method.GetParameters())
                    {
                        CommandParameterSpecification pspec = null;
                        var paramAttr = parameter.GetCustomAttribute<ParameterAttribute>();
                        object defval = null;
                        if (!parameter.HasDefaultValue && parameter.ParameterType.IsValueType)
                            defval = Activator.CreateInstance(parameter.ParameterType);

                        if (paramAttr != null)
                        {
                            // TODO: validate command specification (eg. indexs validity)
                            pspec = new CommandParameterSpecification(
                                parameter.Name,
                                paramAttr.Description,
                                paramAttr.IsOptional,
                                paramAttr.Index,
                                null,
                                true,
                                parameter.HasDefaultValue,
                                (parameter.HasDefaultValue) ? parameter.DefaultValue : defval,
                                parameter);
                        }
                        var optAttr = parameter.GetCustomAttribute<OptionAttribute>();
                        if (optAttr!=null)
                        {
                            var reqParamAttr = parameter.GetCustomAttribute<OptionRequireParameterAttribute>();
                            try
                            {
                                pspec = new CommandParameterSpecification(
                                    parameter.Name,
                                    optAttr.Description,
                                    optAttr.IsOptional,
                                    -1,
                                    optAttr.OptionName ?? parameter.Name,
                                    optAttr.HasValue,
                                    parameter.HasDefaultValue,
                                    (parameter.HasDefaultValue) ? parameter.DefaultValue : defval,
                                    parameter,
                                    reqParamAttr?.RequiredParameterName);
                            } catch (Exception ex)
                            {
                                Errorln(ex.Message);
                            }
                        }
                        if (pspec == null)
                        {
                            syntaxError = true;
                            Errorln($"invalid parameter: class={type.FullName} method={method.Name} name={parameter.Name}");
                        }
                        else
                            paramspecs.Add(pspec);
                    }

                    if (!syntaxError)
                    {
                        var cmdNameAttr = method.GetCustomAttribute<CommandNameAttribute>();

                        var cmdName = (cmdNameAttr != null && cmdNameAttr.Name != null) ? cmdNameAttr.Name
                            : (cmd.Name ?? method.Name.ToLower());

                        var cmdspec = new CommandSpecification(
                            cmdName,
                            cmd.Description,
                            method,
                            instance,
                            paramspecs);

                        bool registered = true;
                        if (_commands.TryGetValue(cmdspec.Name, out var cmdlst))
                        {
                            if (cmdlst.Select(x => x.MethodInfo.DeclaringType == type).Any())
                            {
                                Errorln($"command already registered: '{cmdspec.Name}' in type '{cmdspec.DeclaringTypeFullName}'");
                                registered = false;
                            }
                            else
                                cmdlst.Add(cmdspec);
                        }
                        else
                            _commands.Add(cmdspec.Name, new List<CommandSpecification> { cmdspec });

                        if (registered)
                        {
                            _syntaxAnalyzer.Add(cmdspec);
                            comsCount++;
                        }
                    }
                }
            }
            if (registerAsModule)
            {
                if (comsCount == 0)
                    Errorln($"no commands found in type '{type.FullName}'");
                else
                {
                    var descAttr = type.GetCustomAttribute<CommandsAttribute>();
                    var description = descAttr != null ? descAttr.Description : "";
                    _modules.Add(type.FullName, new CommandsModule(CommandsModule.DeclaringTypeShortName(type), description, type.Assembly, 1, comsCount, type));
                }
            }
            return comsCount;
        }

        #endregion

        #region command processor session operations

        public static string UserProfileFolder => Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        #endregion

        #region commands operations

        /// <summary>
        /// 1. parse command line
        ///     A. internal command (modules)
        ///     B. underlying shell command
        ///     C. unknown command
        /// 2. execute command or error
        /// </summary>
        /// <param name="expr">expression to be evaluated</param>
        /// <returns>return code</returns>
        public static ExpressionEvaluationResult Eval(string expr,int outputX)
        {
            var parseResult = Parse(_syntaxAnalyzer,expr);
            ExpressionEvaluationResult r = null;
            var errorText = "";

            switch (parseResult.ParseResultType)
            {
                case ParseResultType.Valid:
                    var syntaxParsingResult = parseResult.SyntaxParsingResults.First();
                    try
                    {
                        syntaxParsingResult.CommandSyntax.Invoke(syntaxParsingResult.MatchingParameters);
                    } catch (Exception commandInvokeError)
                    {
                        var commandError = commandInvokeError.InnerException ?? commandInvokeError;
                        Errorln(commandError.Message);
                        return new ExpressionEvaluationResult(null, parseResult.ParseResultType, null, ReturnCodeError, commandError);
                    }
                    break;

                case ParseResultType.Empty:
                    r = new ExpressionEvaluationResult(null, parseResult.ParseResultType, null, ReturnCodeOK, null);
                    break;

                case ParseResultType.NotValid:
                    var perComErrs = new Dictionary<string, List<CommandSyntaxParsingResult>>();
                    foreach (var prs in parseResult.SyntaxParsingResults)
                        if (perComErrs.TryGetValue(prs.CommandSyntax.CommandSpecification.Name, out var lst))
                            lst.Add(prs);
                        else
                            perComErrs.Add(prs.CommandSyntax.CommandSpecification.Name,new List<CommandSyntaxParsingResult> { prs });

                    var errs = new List<string>();
                    var minErrPosition = int.MaxValue;
                    var errPositions = new List<int>();
                    foreach (var kvp in perComErrs)
                    {
                        var comSyntax = kvp.Value.First().CommandSyntax;
                        foreach (var prs in kvp.Value)
                        {
                            foreach (var perr in prs.ParseErrors)
                            {
                                minErrPosition = Math.Min(minErrPosition, perr.Position);
                                errPositions.Add(perr.Position);
                                if (!errs.Contains(perr.Description))
                                    errs.Add(perr.Description);
                            }
                            errorText += Br + string.Join(Br, errs);
                        }
                        errorText += $"{Br}for syntax: {comSyntax}{Br}";
                    }

                    errPositions.Sort();
                    errPositions = errPositions.Distinct().ToList();

                    var t = new string[expr.Length + 2];
                    for (int i = 0; i < t.Length; i++) t[i] = " ";
                    foreach (var idx in errPositions)
                    {
                        t[GetIndex(idx, expr)] = $"^";
                    }
                    var serr = string.Join("", t);
                    Error(" ".PadLeft(outputX + 1) + serr);

                    Error(errorText);
                    r = new ExpressionEvaluationResult(errorText, parseResult.ParseResultType, null, ReturnCodeNotDefined, null);
                    break;

                case ParseResultType.Ambiguous:
                    errorText += $"{Red}ambiguous syntaxes:{Br}";
                    foreach (var prs in parseResult.SyntaxParsingResults)
                        errorText += $"{Red}{prs.CommandSyntax}{Br}";
                    Print(errorText);
                    r = new ExpressionEvaluationResult(errorText, parseResult.ParseResultType, null, ReturnCodeNotDefined, null);
                    break;

                case ParseResultType.NotIdentified:
                    r = new ExpressionEvaluationResult(null, parseResult.ParseResultType, null, ReturnCodeNotDefined, null);
                    break;
            }

            return r;
        }
        
        #endregion
    }
}
