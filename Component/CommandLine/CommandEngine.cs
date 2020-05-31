using DotNetConsoleSdk.Component.CommandLine.CommandModel;
using DotNetConsoleSdk.Component.CommandLine.Commands;
using static DotNetConsoleSdk.Component.CommandLine.Parsing.CommandLineParser;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Text;
using DotNetConsoleSdk.Component.CommandLine.Parsing;
using System.Linq;

namespace DotNetConsoleSdk.Component.CommandLine
{
    public static class CommandEngine
    {
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

        public static void InitializeCommandEngine(string[] args)
        {
            SetArgs(args);
            if (!_isInitialized)
            {
                _isInitialized = true;
                RegisterCommandsClass<CommandEngineCommands>();
                RegisterCommandsClass<StdoutCommands>();
            }
        }

        public static void RegisterCommandsModule(string assemblyPath)
        {
            
        }

        static void RegisterCommandsClass<T>() 
        {
            var type = typeof(T);
            object instance = Activator.CreateInstance<T>();            
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            foreach ( var method in methods )
            {
                var cmd = method.GetCustomAttribute<CommandAttribute>();
                if (cmd!=null)
                {
                    var paramspecs = new List<CommandParameterSpecification>();
                    foreach ( var parameter in method.GetParameters())
                    {
                        var p = parameter.GetCustomAttribute<ParameterAttribute>() ?? throw new ArgumentException($"invalid parameter: class={type.FullName} method={method.Name} name={parameter.Name}");
                        // TODO: validate command specification (eg. indexs validity)
                        var pspec = new CommandParameterSpecification(
                            parameter.Name, 
                            p.Description, 
                            parameter.IsOptional, 
                            p.Index, 
                            p.OptionName,
                            parameter.HasDefaultValue,
                            (parameter.HasDefaultValue)? parameter.DefaultValue:null,
                            parameter); ;
                        paramspecs.Add(pspec);
                    }
                    var cmdspec = new CommandSpecification(
                        method.Name.ToLower(), 
                        cmd.Description, 
                        method,
                        instance,
                        paramspecs);
                    if (_commands.TryGetValue(cmdspec.Name, out var cmdlst))
                        cmdlst.Add(cmdspec);
                    else
                        _commands.Add(cmdspec.Name, new List<CommandSpecification> { cmdspec });
                    _syntaxAnalyzer.Add(cmdspec);
                }
            }
        }

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
        public static ExpressionEvaluationResult Eval(string expr)
        {
            var result = Parse(_syntaxAnalyzer,expr);
            ExpressionEvaluationResult r = null;

            switch (result.parseResult)
            {
                case ParseResult.Valid:
                    // TODO: need parameters values (pname->value) (from parser)
                    result.commandSpecifications.First().Invoke();
                    break;
                case ParseResult.Empty:
                    r = new ExpressionEvaluationResult(null, result.parseResult, null, ReturnCodeOK, null);
                    break;
                case ParseResult.NotValid:
                    var syntaxError = "";
                    r = new ExpressionEvaluationResult(syntaxError, result.parseResult, null, ReturnCodeNotDefined, null);
                    break;
                case ParseResult.Ambiguous:
                    var ambiguousSyntaxError = "";
                    r = new ExpressionEvaluationResult(ambiguousSyntaxError, result.parseResult, null, ReturnCodeNotDefined, null);
                    break;
                case ParseResult.NotIdentified:
                    r = new ExpressionEvaluationResult(null, result.parseResult, null, ReturnCodeNotDefined, null);
                    break;
            }

            return r;
        }
        
        #endregion
    }
}
