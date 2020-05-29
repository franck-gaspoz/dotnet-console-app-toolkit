using DotNetConsoleSdk.Component.CommandLine.CommandModel;
using DotNetConsoleSdk.Component.CommandLine.Commands;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace DotNetConsoleSdk.Component.CommandLine
{
    public static class CommandEngine
    {
        public static int ReturnCodeOK = 0;
        public static int ReturnCodeError = 1;

        static string[] _args;

        static readonly Dictionary<string, CommandSpecification> _commands = new Dictionary<string, CommandSpecification>();

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
            RegisterClass(typeof(CommandEngineCommands));
            RegisterClass(typeof(StdoutCommands));
        }

        public static void RegisterModule(string assemblyPath)
        {

        }

        public static void RegisterClass(Type type) 
        {
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
                    var cmdspec = new CommandSpecification(method.Name.ToLower(), cmd.Description, method,paramspecs);
                    if (_commands.ContainsKey(cmdspec.Name))
                        throw new Exception($"duplicated command name: class={type.FullName} method={method.Name}");
                    _commands.Add(cmdspec.Name, cmdspec);
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
        public static int Eval(string expr)
        {
            //var splits = SplitExpr(expr);
            
            return ReturnCodeOK;
        }
        
        #endregion
    }
}
