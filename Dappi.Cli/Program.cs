using System.Reflection;
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
                Console.WriteLine();
                // Asynchronous
                await AnsiConsole.Status()
                    .StartAsync("Initializing Dappi Project.", async ctx => 
                    {
                        // Omitted
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