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
    public static class CommandLineProcessor
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
                RegisterCommandsClass<CommandLineProcessorCommands>();
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
                            null,
                            p.Name??parameter.Name,
                            parameter.HasDefaultValue,
                            (parameter.HasDefaultValue) ? parameter.DefaultValue : null,
                            parameter);
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
            var parseResult = Parse(_syntaxAnalyzer,expr);
            ExpressionEvaluationResult r = null;

            switch (parseResult.ParseResultType)
            {
                case ParseResultType.Valid:
                    // TODO: need parameters values (pname->value) (from parser)
                    parseResult.CommandSpecifications.First().Invoke();
                    break;
                case ParseResultType.Empty:
                    r = new ExpressionEvaluationResult(null, parseResult.ParseResultType, null, ReturnCodeOK, null);
                    break;
                case ParseResultType.NotValid:
                    var syntaxError = "";
                    r = new ExpressionEvaluationResult(syntaxError, parseResult.ParseResultType, null, ReturnCodeNotDefined, null);
                    break;
                case ParseResultType.Ambiguous:
                    var ambiguousSyntaxError = "";
                    r = new ExpressionEvaluationResult(ambiguousSyntaxError, parseResult.ParseResultType, null, ReturnCodeNotDefined, null);
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
