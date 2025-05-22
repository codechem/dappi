using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Spectre.Console.Cli;

namespace Dappi.Cli.Commands;

public class StartCommand : Command<StartCommand.Settings>
{
    public const string CommandName = "start";
    public sealed class Settings : LogCommandSettings
    {
        [CommandOption("-p|--path <PROJECT-PATH>")]
        public string? ProjectPath { get; set; }
    }

    public override int Execute(CommandContext context, Settings settings)
    {
        var projectPath = string.IsNullOrEmpty(settings.ProjectPath)
            ? Directory.GetCurrentDirectory()
            : settings.ProjectPath;

        var csProjFile = Directory.GetFiles(projectPath, "*.csproj", SearchOption.AllDirectories)
            .FirstOrDefault()!;

        var startInfo = new ProcessStartInfo()
        {
            WorkingDirectory = projectPath,
            UseShellExecute = false,
            Arguments = $"run --project {csProjFile}",
            FileName = "dotnet",
        };

        var process = Process.Start(startInfo);
        if (process is null)
        {
            // We should fail here, no shame in this case
            throw new InvalidOperationException("Failed to start process.");
        }
        process.WaitForExit();

        return process.ExitCode;
    }
}