using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using McMaster.Extensions.CommandLineUtils;

namespace Dappi.Cli.Commands;

[Command("start", FullName = "Project starter", Description = "Runs your Dappi project")]
[HelpOption]
public class StartCommand
{
    [Option("-p|--path <PATH>", "The path to where your new project should be initialized. Defaults to the current directory.", CommandOptionType.SingleValue)]
    public string ProjectPath { get; set; }
    
    private void OnExecute(CommandLineApplication app)
    {
        Console.WriteLine("Running start");
        var projectPath = GetProjectPath();
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

    private string GetProjectPath()
    {
        var projectPath = string.IsNullOrEmpty(ProjectPath) ? Directory.GetCurrentDirectory() : ProjectPath;

        if (Directory.GetCurrentDirectory().EndsWith("InventoryTestis123"))
        {
            return projectPath;
        }

        var csProjFile = Directory.GetFiles(projectPath, "*.sln", SearchOption.AllDirectories).FirstOrDefault();
        return Path.GetDirectoryName(csProjFile);
    }
}