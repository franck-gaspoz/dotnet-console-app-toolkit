//#define enable_test_commands

using DotNetConsoleAppToolkit.Component.CommandLine.CommandModel;
using DotNetConsoleAppToolkit.Component.CommandLine.Parsing;
using DotNetConsoleAppToolkit.Console;
using DotNetConsoleAppToolkit.Lib;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using static DotNetConsoleAppToolkit.Component.CommandLine.Parsing.CommandLineParser;
using static DotNetConsoleAppToolkit.DotNetConsole;
using cmdlr = DotNetConsoleAppToolkit.Component.CommandLine.CommandLineReader;
using cons = System.Console;

namespace DotNetConsoleAppToolkit.Component.CommandLine
{
    public class CommandLineProcessor
    {
        public const string AppName = "? sh";
        public const string AppLongName = "? Shell";
        public const string AppEditor = "released on June 2020 under licence MIT";

        #region attributes

        public CancellationTokenSource CancellationTokenSource;

        /*public const int ReturnCodeOK = 0;
        public const int ReturnCodeError = 1;
        public const int ReturnCodeNotDefined = 1;*/

        string[] _args;
        bool _isInitialized = false;

        readonly Dictionary<string, List<CommandSpecification>> _commands = new Dictionary<string, List<CommandSpecification>>();

        public ReadOnlyDictionary<string, List<CommandSpecification>> Commands => new ReadOnlyDictionary<string, List<CommandSpecification>>(_commands);

        public List<CommandSpecification> AllCommands {
            get {
                var coms = new List<CommandSpecification>();
                foreach (var kvp in _commands)
                    foreach (var com in kvp.Value)
                        coms.Add(com);
                coms.Sort(new Comparison<CommandSpecification>((x, y) => x.Name.CompareTo(y.Name)));
                return coms;
            }
        }

        readonly SyntaxAnalyser _syntaxAnalyzer = new SyntaxAnalyser();

        readonly Dictionary<string, CommandsModule> _modules = new Dictionary<string, CommandsModule>();

        public IReadOnlyDictionary<string, CommandsModule> Modules => new ReadOnlyDictionary<string, CommandsModule>(_modules);

        public IEnumerable<string> CommandDeclaringShortTypesNames => AllCommands.Select(x => x.DeclaringTypeShortName).Distinct();
        public IEnumerable<string> CommandDeclaringTypesNames => AllCommands.Select(x => x.DeclaringTypeFullName).Distinct();

        public CommandsHistory CmdsHistory { get; protected set; }

        public cmdlr.CommandLineReader CmdLineReader { get; set; }

        public CommandEvaluationContext CommandEvaluationContext { get; protected set; }

        #endregion

        #region cli methods

        public string Arg(int n)
        {
            if (_args == null) return null;
            if (_args.Length <= n) return null;
            return _args[n];
        }

        public bool HasArgs => _args != null && _args.Length > 0;

        void SetArgs(string[] args)
        {
            _args = (string[])args?.Clone();
        }

        #endregion

        #region command engine operations

        public CommandLineProcessor(
            string[] args, 
            bool printInfo = true,
            CommandEvaluationContext commandEvaluationContext = null
            )
        {
            InitializeCommandProcessor(args, printInfo, commandEvaluationContext);
        }

        void InitializeCommandProcessor(string[] args, bool printInfo=true, CommandEvaluationContext commandEvaluationContext = null)
        {
            SetArgs(args);
            if (!_isInitialized)
            {
                _isInitialized = true;

                cons.ForegroundColor = DefaultForeground;
                cons.BackgroundColor = DefaultBackground;
                                
                CommandEvaluationContext = commandEvaluationContext ??
                    new CommandEvaluationContext(
                        this,
                        Out,
                        cons.In,
                        Err,
                        null
                    );

                if (printInfo) PrintInfo(CommandEvaluationContext);

                CmdsHistory = new CommandsHistory(UserProfileFolder);

                RegisterCommandsAssembly(CommandEvaluationContext,Assembly.GetExecutingAssembly());
#if enable_test_commands
                RegisterCommandsClass<TestCommands>();
#endif
            }
        }

        public void AssertCommandLineProcessorHasACommandLineReader()
        {
            if (CmdLineReader == null) throw new Exception("a command line reader is required by the command line processor to perform this action");
        }

        public void PrintInfo(CommandEvaluationContext context)
        {
            context.Out.Println($"{ColorSettings.Label}{Uon} {AppLongName} ({AppName}) version {Assembly.GetExecutingAssembly().GetName().Version}" + ("".PadRight(18,' ')) + Tdoff);
            context.Out.Println($" {AppEditor}");
            context.Out.Println($" {RuntimeInformation.OSDescription} {RuntimeInformation.OSArchitecture} - {RuntimeInformation.FrameworkDescription}");
            context.Out.Println();
        }
        
        public (int typesCount,int commandsCount) 
            UnregisterCommandsAssembly(
            CommandEvaluationContext context,
            string assemblyName)
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

        bool RemoveCommand(CommandSpecification comSpec)
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

        public (int typesCount, int commandsCount) RegisterCommandsAssembly(
            CommandEvaluationContext context,
            string assemblyPath)
        {
            var assembly = Assembly.LoadFrom(assemblyPath);
            return RegisterCommandsAssembly(context,assembly);
        }

        public (int typesCount,int commandsCount) RegisterCommandsAssembly(
            CommandEvaluationContext context,
            Assembly assembly)
        {
            if (_modules.ContainsKey(assembly.ManifestModule.Name))
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
                if (comsAttr != null && type.GetInterface(typeof(ICommandsDeclaringType).FullName)!=null )
                    comCount = RegisterCommandsClass(context,type,false);                
                if (comCount > 0)
                    typesCount++;
                comTotCount += comCount;
            }
            if (typesCount > 0)
            {
                var descAttr = assembly.GetCustomAttribute<AssemblyDescriptionAttribute>();
                var description = (descAttr != null) ? descAttr.Description : "";
                _modules.Add(Path.GetFileNameWithoutExtension(assembly.ManifestModule.Name), new CommandsModule(Path.GetFileNameWithoutExtension(assembly.Location), description, assembly, typesCount, comTotCount));
            }
            return (typesCount,comTotCount);    
        }

        public void RegisterCommandsClass<T>(CommandEvaluationContext context) => RegisterCommandsClass(context,typeof(T),true);

        public int RegisterCommandsClass(CommandEvaluationContext context, Type type) => RegisterCommandsClass(context,type, true);

        int RegisterCommandsClass(CommandEvaluationContext context, Type type,bool registerAsModule)
        {
            if (type.GetInterface(typeof(ICommandsDeclaringType).FullName)==null)
                throw new Exception($"the type '{type.FullName}' must implements interface '{typeof(ICommandsDeclaringType).FullName}' to be registered as a command class");
            var comsCount = 0;
            object instance = Activator.CreateInstance(type,new object[] { });            
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            if (registerAsModule && _modules.ContainsKey(type.FullName))
            {
                Errorln($"a module with same name than commands type '{type.FullName}' is already registered");
                return 0;
            }
            foreach ( var method in methods )
            {
                var cmd = method.GetCustomAttribute<CommandAttribute>();
                if (cmd!=null)
                {
                    var paramspecs = new List<CommandParameterSpecification>();
                    bool syntaxError = false;
                    var pindex = 0;
                    foreach ( var parameter in method.GetParameters())
                    {
                        if (pindex == 0)
                        {
                            // manadatory: param 0 is CommandEvaluationContext
                            if (parameter.ParameterType != typeof(CommandEvaluationContext))
                            {
                                Errorln($"class={type.FullName} method={method.Name} parameter 0 ('{parameter.Name}') should be of type '{typeof(CommandEvaluationContext).FullName}', but is of type: {parameter.ParameterType.FullName}");
                                syntaxError = true;
                                break;
                            }
                        }
                        else
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
                            if (optAttr != null)
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
                                }
                                catch (Exception ex)
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
                        pindex++;
                    }

                    if (!syntaxError)
                    {
                        var cmdNameAttr = method.GetCustomAttribute<CommandNameAttribute>();

                        var cmdName = (cmdNameAttr != null && cmdNameAttr.Name != null) ? cmdNameAttr.Name
                            : (cmd.Name ?? method.Name.ToLower());

                        var cmdspec = new CommandSpecification(
                            cmdName,
                            cmd.Description,
                            cmd.LongDescription,
                            cmd.Documentation,
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

        public string UserProfileFolder => Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

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
        public ExpressionEvaluationResult Eval(string expr,int outputX)
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
                        var outputData = InvokeCommand(CommandEvaluationContext, syntaxParsingResult.CommandSyntax.CommandSpecification, syntaxParsingResult.MatchingParameters);
                        
                        r = new ExpressionEvaluationResult(null, ParseResultType.Valid, outputData, (int)ReturnCode.OK, null);
                    } catch (Exception commandInvokeError)
                    {
                        var commandError = commandInvokeError.InnerException ?? commandInvokeError;
                        Errorln(commandError.Message);
                        return new ExpressionEvaluationResult(null, parseResult.ParseResultType, null, (int)ReturnCode.Error, commandError);
                    }
                    break;

                case ParseResultType.Empty:
                    r = new ExpressionEvaluationResult(null, parseResult.ParseResultType, null, (int)ReturnCode.OK, null);
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
                            errorText += Br + Red + string.Join(Br+Red, errs);
                        }
                        errorText += $"{Br}{Red}for syntax: {comSyntax}{Br}";
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
                    r = new ExpressionEvaluationResult(errorText, parseResult.ParseResultType, null, (int)ReturnCode.NotDefined, null);
                    break;

                case ParseResultType.Ambiguous:
                    errorText += $"{Red}ambiguous syntaxes:{Br}";
                    foreach (var prs in parseResult.SyntaxParsingResults)
                        errorText += $"{Red}{prs.CommandSyntax}{Br}";
                    Error(errorText);
                    r = new ExpressionEvaluationResult(errorText, parseResult.ParseResultType, null, (int)ReturnCode.NotDefined, null);
                    break;

                case ParseResultType.NotIdentified:
                    r = new ExpressionEvaluationResult(null, parseResult.ParseResultType, null, (int)ReturnCode.NotDefined, null);
                    break;
            }

            return r;
        }

        object InvokeCommand(
            CommandEvaluationContext context,
            CommandSpecification commandSpecification,
            MatchingParameters matchingParameters)
        {
            var parameters = new List<object>() { context };
            var pindex = 0;
            foreach (var parameter in commandSpecification.MethodInfo.GetParameters())
            {
                if (pindex > 0)
                {
                    if (matchingParameters.TryGet(parameter.Name, out var matchingParameter))
                        parameters.Add(matchingParameter.GetValue());
                    else
                        throw new InvalidOperationException($"parameter not found: '{parameter.Name}' when invoking command: {commandSpecification}");
                }
                pindex++;
            }
            var r = commandSpecification.MethodInfo
                .Invoke(commandSpecification.MethodOwner, parameters.ToArray());
            return r;
        }
                
        #endregion
    }
}
