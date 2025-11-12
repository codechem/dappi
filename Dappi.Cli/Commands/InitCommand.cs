using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Dappi.Cli.Exceptions;
using Dappi.Cli.Helpers;
using Microsoft.Build.Construction;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Dappi.Cli.Commands;

public class InitCommand(ILogger<InitCommand> logger) : AsyncCommand<InitCommand.Settings>
{
    public const string CommandName = "init";
    public sealed class Settings : LogCommandSettings
    {
        [CommandOption("-n|--name <PROJECT-NAME>")]
        public string? ProjectName { get; set; }

        [CommandOption("-p|--path <OUTPUT-PATH>")]
        public string? OutputPath { get; set; }

        [CommandOption("--use-prerelease", IsHidden = true)]
        public bool UsePreRelease { get; set; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        if (settings.ProjectName is null)
        {
            return -1;
        }

        try
        {
            logger.LogInformation("Creating project {ProjectName}", settings.ProjectName);

            string projectPath;
            if (string.IsNullOrEmpty(settings.OutputPath))
            {
                projectPath = Directory.GetCurrentDirectory();
            }
            else
            {
                // Expand tilde to home directory if present
                var outputPath = settings.OutputPath;
                if (outputPath.StartsWith("~"))
                {
                    var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                    if (outputPath.Length == 1)
                    {
                        outputPath = homeDir;
                    }
                    else if (outputPath.Length > 1 && (outputPath[1] == '/' || outputPath[1] == '\\'))
                    {
                        outputPath = Path.Combine(homeDir, outputPath.Substring(2));
                    }
                    else
                    {
                        outputPath = Path.Combine(homeDir, outputPath.Substring(1));
                    }
                }

                // Ensure we're getting an absolute path
                projectPath = Path.GetFullPath(outputPath);
            }

            var template = await TemplateFetcher.GetDappiTemplate(usePreRelease: settings.UsePreRelease, logger);

            var outputFolder = Path.Combine(projectPath, settings.ProjectName);

            logger.LogDebug("Output folder will be {OutputFolder}", outputFolder);

            ZipHelper.ExtractZipFile(template.physicalPath, outputFolder, "templates");

            var oldSolutionFile = Path.Combine(outputFolder, $"{Constants.ProjectNamePlaceholder}.sln");

            if (File.Exists(oldSolutionFile))
                File.Delete(Path.Combine(outputFolder, $"{Constants.ProjectNamePlaceholder}.sln"));

            RenameHelper.RenameFolders(outputFolder, Constants.ProjectNamePlaceholder, settings.ProjectName, excludedSubFolders: []);

            var csProjFile = Directory.GetFiles(outputFolder, "*.csproj", SearchOption.AllDirectories).FirstOrDefault()!;

            ModifyCsprojWithNugetReferences(template, csProjFile);

            AnsiConsole.Status()
                .Start($"Creating your solution {settings.ProjectName}", ctx =>
                {
                    ctx.Spinner(Spinner.Known.Circle);

                    var procStartInfo = new ProcessStartInfo()
                    {
                        FileName = "dotnet",
                        Arguments = $"new sln -n \"{settings.ProjectName}\"",
                        WorkingDirectory = outputFolder,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                    };

                    var process = Process.Start(procStartInfo);
                    process?.WaitForExit();

                    if (process?.ExitCode != Environment.ExitCode)
                    {
                        throw new DappiInitializationFailedException(
                            $"Command: {procStartInfo.FileName} Arguments: {procStartInfo.Arguments} {process?.StandardError.ReadToEnd()}");
                    }

                    var addProjectToSlnStartInfo = new ProcessStartInfo()
                    {
                        FileName = "dotnet",
                        Arguments = $"sln add \"{csProjFile}\"",
                        WorkingDirectory = outputFolder,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                    };

                    var processAdd = Process.Start(addProjectToSlnStartInfo);
                    processAdd?.WaitForExit();

                    if (processAdd?.ExitCode != Environment.ExitCode)
                    {
                        throw new DappiInitializationFailedException(
                            $"{addProjectToSlnStartInfo.FileName}: {addProjectToSlnStartInfo.Arguments} output: {processAdd?.StandardError.ReadToEnd()}");
                    }
                });

            AnsiConsole.Status()
                .Start("Generating initial migrations...", ctx =>
                {
                    ctx.Spinner(Spinner.Known.Circle);

                    var procStartInfo = new ProcessStartInfo()
                    {
                        UseShellExecute = false,
                        WorkingDirectory = Path.GetDirectoryName(outputFolder),
                        FileName = "dotnet",
                        Arguments = "ef migrations add Dappi_InitialMigration --project " + csProjFile,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                    };

                    var process = Process.Start(procStartInfo);
                    process?.WaitForExit();
                    if (process?.ExitCode != Environment.ExitCode)
                    {
                        throw new DappiInitializationFailedException(
                            $"{procStartInfo.FileName}: {procStartInfo.Arguments} output: {process?.StandardOutput.ReadToEnd()}");
                    }
                });

            logger.LogInformation("Your Dappi project has been initialized in {ProjectPath}", outputFolder);
            logger.LogInformation(
                "You can start your project by using {DappiStartCommand} or navigate to {ProjectPath} and run your project through dotnet or your IDE of choice ",
                $"dappi {StartCommand.CommandName} --path {outputFolder}", outputFolder);

            return 0;
        }
        catch (Exception e)
        {
            logger.LogInformation("Dappi Initialization failed");
            logger.LogError(e, e.Message);
            return -1;
        }
    }

    private static void ModifyCsprojWithNugetReferences((string physicalPath, string tagName) template, string csProjFile)
    {
        var referenceReplacements = GetReplacementsForProjectReferences(template.tagName);
        var project = ProjectRootElement.Open(csProjFile)!;
        var projectReferences =
            project.Items.Where(i => i.ElementName == "ProjectReference").ToList();

        var initialPackagesParent = projectReferences.FirstOrDefault()!.Parent;
        var newItemGroup = project.AddItemGroup();

        foreach (var projectReference in projectReferences)
        {
            var replacement = referenceReplacements[projectReference.Include];
            newItemGroup.AddItem("PackageReference", replacement.Item1,
                replacement.Item2.Select(x => new KeyValuePair<string, string>(x.Item1, x.Item2)));
        }

        project.RemoveChild(initialPackagesParent);
        project.Save();
    }

    private static Dictionary<string, (string, (string, string)[])> GetReplacementsForProjectReferences(string tagName) =>
        new()
        {
            {
                @"..\..\Dappi.HeadlessCms\Dappi.HeadlessCms.csproj",
                ("Dappi.HeadlessCms", [("Version", tagName)])
            },
            {
                @"..\..\Dappi.SourceGenerator\Dappi.SourceGenerator.csproj",
                ("Dappi.SourceGenerator", [("Version", tagName), ("OutputItemType", "Analyzer")])
            },
            {
                @"..\..\Dappi.Core\Dappi.Core.csproj",
                ("Dappi.Core", [("Version", tagName)])
            }
        };
}