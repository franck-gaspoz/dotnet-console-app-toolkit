﻿#define enable_test_commands

using DotNetConsoleSdk.Component.CommandLine.CommandModel;
using DotNetConsoleSdk.Component.CommandLine.Commands;
using static DotNetConsoleSdk.DotNetConsole;
using static DotNetConsoleSdk.Component.CommandLine.Parsing.CommandLineParser;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Text;
using DotNetConsoleSdk.Component.CommandLine.Parsing;
using System.Linq;
using System.Numerics;

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
                RegisterCommandsClass<ConsoleCommands>();
#if enable_test_commands
                RegisterCommandsClass<TestCommands>();
#endif
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
                        CommandParameterSpecification pspec = null;
                        var paramAttr = parameter.GetCustomAttribute<ParameterAttribute>();
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
                                (parameter.HasDefaultValue) ? parameter.DefaultValue : null,
                                parameter);
                        }
                        var optAttr = parameter.GetCustomAttribute<OptionAttribute>();
                        if (optAttr!=null)
                        {
                            pspec = new CommandParameterSpecification(
                                parameter.Name,
                                optAttr.Description,
                                optAttr.IsOptional,
                                -1,
                                optAttr.OptionName ?? parameter.Name,
                                optAttr.HasValue,
                                parameter.HasDefaultValue,
                                (parameter.HasDefaultValue) ? parameter.DefaultValue : null,
                                parameter);
                        }
                        if (pspec==null)
                            throw new ArgumentException($"invalid parameter: class={type.FullName} method={method.Name} name={parameter.Name}");
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
        public static ExpressionEvaluationResult Eval(string expr,int outputX)
        {
            var parseResult = Parse(_syntaxAnalyzer,expr);
            ExpressionEvaluationResult r = null;
            var errorText = "";

            switch (parseResult.ParseResultType)
            {
                case ParseResultType.Valid:
                    var syntaxParsingResult = parseResult.SyntaxParsingResults.First();
                    syntaxParsingResult.CommandSyntax.Invoke(syntaxParsingResult.MatchingParameters);
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
                            if (string.IsNullOrWhiteSpace(errorText))
                                errorText += Red;
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
                        t[GetIndex(idx, expr)] = $"{Red}^";
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
