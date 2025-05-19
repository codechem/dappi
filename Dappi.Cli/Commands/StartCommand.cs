using System.Diagnostics;
using System.IO;
using McMaster.Extensions.CommandLineUtils;

namespace Dappi.Cli.Commands;

[Command("start", FullName = "Project starter", Description = "Runs your Dappi project")]
[HelpOption]
public class StartCommand
{
    [Option("-p|--path <PATH>", "The path to where your new project should be initialized. Defaults to the current directory.", CommandOptionType.SingleValue)]
    public string? ProjectPath { get; set; }
    
    private void OnExecute(CommandLineApplication _)
    {
        var projectPath = string.IsNullOrEmpty(ProjectPath) ? Directory.GetCurrentDirectory() : ProjectPath;
        var startInfo = new ProcessStartInfo()
        {
            WorkingDirectory = projectPath,
            UseShellExecute = false,
            Arguments = $"run",
            FileName = "dotnet",
        };
        var process = Process.Start(startInfo);
        process?.WaitForExit();
    }
}