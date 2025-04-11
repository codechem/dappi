using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using CommandLine;
using CommandLine.Text;
using Spectre.Console;

namespace Dappi.Cli;

public static class Program
{
    private const string DappiCliLogo = @"
   ____                    _ 
  |  _ \  __ _ _ __  _ __ (_)
  | | | |/ _` | '_ \| '_ \| |
  | |_| | (_| | |_) | |_) | |
  |____/ \__,_| .__/| .__/|_|
              |_|   |_|      
";

    [Verb("init" , HelpText = "Initialize a Dappi project")]
    private class InitCommand
    {
        [Option('n', "project-name", Required = true, HelpText = "Project name")]
        public string? ProjectName { get; set; }
       
        [Option('p', "path", Required = true, HelpText = "Where to initialize the Dappi project")]
        public string? ProjectPath { get; set; }
    }
    
    [Verb("start" , HelpText = "Start the Dappi")]
    private class StartCommand
    {
        [Option('p', "path", Required = true, HelpText = "Path to the Dappi project")]
        public string? ProjectPath { get; set; }
    }
    
    public static async Task Main(string[] args)
    {
        var dappiHeader = GetDappiHeaderWithVersion();

        var parser = new Parser(with => with.HelpWriter = null);
        var parserResult = parser.ParseArguments<InitCommand, StartCommand>(args);
        var helpText = BuildAndGetHelpText(parserResult, dappiHeader);
        
        parserResult.WithNotParsed(result =>
            {
                Console.WriteLine(helpText);
            })
            .WithParsed<InitCommand>(async command =>
            {
                await AnsiConsole.Status()
                    .StartAsync("Initializing Dappi Project.", async ctx =>
                    {
                        var templateFetcher = new TemplateFetcher();
                        await templateFetcher.GetDappiTemplate();
                        // var startInfo = new ProcessStartInfo
                        // {
                        //     FileName = "dotnet",
                        //     Arguments = $"new webapi -n {command.ProjectName} --force",
                        //     WorkingDirectory = command.ProjectPath,
                        //     RedirectStandardOutput = true,
                        //     RedirectStandardError = true,
                        //     UseShellExecute = false,
                        //     CreateNoWindow = true
                        // };
                        //
                        // using var process = new Process();
                        // process.StartInfo = startInfo;
                        // process.OutputDataReceived += (sender, e) => Console.WriteLine(e.Data);
                        // process.ErrorDataReceived += (sender, e) => Console.Error.WriteLine(e.Data);
                        // process.Start();
                        // process.BeginOutputReadLine();
                        // process.BeginErrorReadLine();
                        // process.WaitForExit();
                        //
                        // var startInfo1 = new ProcessStartInfo
                        // {
                        //     FileName = "dotnet",
                        //     WorkingDirectory = command.ProjectPath,
                        //     Arguments = $"add {command.ProjectPath}\\{command.ProjectName}\\{command.ProjectName}.csproj package Dappi.Extensions.DependencyInjection --source C:\\LocalNuget ",
                        //     RedirectStandardOutput = true,
                        //     RedirectStandardError = true,
                        //     UseShellExecute = false,
                        //     CreateNoWindow = true
                        // };
                        //
                        // using var process1 = new Process();
                        // process1.StartInfo = startInfo1;
                        // process1.OutputDataReceived += (sender, e) => Console.WriteLine(e.Data);
                        // process1.ErrorDataReceived += (sender, e) => Console.Error.WriteLine(e.Data);
                        // process1.Start();
                        // process1.BeginOutputReadLine();
                        // process1.BeginErrorReadLine();
                        // process1.WaitForExit();
                    });
                
                    
            })
            .WithParsed<StartCommand>(command =>
            {
                Console.WriteLine(dappiHeader);
                Console.WriteLine($"Starting Dappi project at {command.ProjectPath}");
            });

    }

    private static string GetDappiHeaderWithVersion()
    {
        var versionString = Assembly.GetEntryAssembly()?
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;

        var dappiHeader = DappiCliLogo + Environment.NewLine + $"Dotnet API Pre-Programming Interface {versionString}";
        return dappiHeader;
    }

    private static HelpText BuildAndGetHelpText(ParserResult<object> parserResult, string dappiHeader)
    {
        var helpText = HelpText.AutoBuild(parserResult, h =>
        {
            h.AdditionalNewLineAfterOption = false;
            h.Heading = dappiHeader;
            h.Copyright = "Codechem @2025";
            h.AutoVersion = false;
            return h;
        }, e => e);
        return helpText;
    }
}