# Dot Net Console App Toolkit
<b>Dot Net Console App Toolkit</b> helps build fastly nice multi-plateforms (windows, linux, macos) console applications using C# and .NET Core 3.1 and .NET Standard 2.1

[![licence mit](https://img.shields.io/badge/licence-MIT-blue.svg)](license.md) This project is licensed under the terms of the MIT license: [LICENSE.md](LICENSE.md)

# Features

The toolkit provides functionalities needed to build console applications running in a terminal (WSL/WSL2, cmd.exe, ConEmu, bash, ...) with text interface. That includes:
- <b>a text printer engine </b>that supports <b>print directives</b> allowing to manage console functionalities from text itself, as html would do but with a simplest grammar (that can be configured). That makes possible colored outputs, cursor control, text scrolling and so on, based on several APIs like System.Console and VT100. The print directives are available as commands strings, as a command from the integrated shell or from an underlying shell
- <b>UI controls</b> for displaying texts and graphical characters in a various way and handling user inputs
- <b>command line analyser / interpretor</b> with an adaptable syntax, that can interact with any underlying shell, allowing to build either a stand alone shell or a shell extension
- <b>a simple way to define shell commands</b> using C# method and parameters attributes, avoiding the developer to handle syntax analyzing and shell integration (command help, pipelines, standard stream redirections) councerns


## Example : Dot Net Console Toolkit integrated Shell 

<img src="Doc/2020-06-13 02_34_57-Window-github.png"/>

This is a view of what is done with the C# project <a href="https://github.com/franck-gaspoz/dotnet-console-app-toolkit-shell"><b>dotnet-console-app-toolkit-shell</b></a>. The <b>Dot Net Console App Toolkit</b> integrates anything needed to run a complete shell, writes shell commands using C# and use console UI components.

> ### :information_source: &nbsp;&nbsp;&nbsp;&nbsp;How this exemple is coded ?
> This shell example runs with just a few lines of code:

``` csharp
    var commandLineReader = new CommandLineReader();
    InitializeCommandProcessor(args,commandLineReader);
    var returnCode = commandLineReader.ReadCommandLine();
    Environment.Exit(returnCode);
```

## packages dependencies:

Microsoft.CodeAnalysis.CSharp.Scripting 3.7.0-1.final
