using System.Diagnostics;
using System.IO;
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

        var startInfo = new ProcessStartInfo()
        {
            WorkingDirectory = projectPath,
            UseShellExecute = false,
            Arguments = $"run",
            FileName = "dotnet",
        };

        var process = Process.Start(startInfo);
        process?.WaitForExit();
        
        return process!.ExitCode;
    }
}