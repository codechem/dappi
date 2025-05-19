using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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

            var projectPath = string.IsNullOrEmpty(settings.OutputPath) ? Directory.GetCurrentDirectory() : settings.OutputPath;
     
            var template = await TemplateFetcher.GetDappiTemplate(usePreRelease: settings.UsePreRelease, logger);
        
            var outputFolder = Path.Combine(projectPath, settings.ProjectName);
            
            logger.LogDebug("Output folder will be {OutputFolder}", outputFolder);

            ZipHelper.ExtractZipFile(template.physicalPath, outputFolder, "templates");
        
            RenameHelper.RenameFolders(outputFolder, Constants.ProjectNamePlaceholder, settings.ProjectName, excludedSubFolders: []);

            var csProjFile = Directory.GetFiles(outputFolder, "*.csproj", SearchOption.AllDirectories).FirstOrDefault()!;

            ModifyCsprojWithNugetReferences(template, csProjFile);
            var process = new Process();
            
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
                    
                    process = Process.Start(procStartInfo);
                    process?.WaitForExit();
                });
            
            logger.LogDebug("dotnet ef output: {EfOutput}", process?.StandardOutput.ReadToEnd().Trim());
            if (!string.IsNullOrWhiteSpace(process?.StandardError.ReadToEnd()))
            {
                logger.LogError("dotnet ef output {EfOutput}", process?.StandardError.ReadToEnd().Trim());
            }
            
            logger.LogInformation("Dappi Initialization finished {ProjectPath}", outputFolder);

            return 0;
        }
        catch (Exception e)
        {
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
                @"..\..\CCApi.Extensions.DependencyInjection\CCApi.Extensions.DependencyInjection.csproj",
                ("Dappi.HeadlessCms", [("Version", tagName)])
            },
            {
                @"..\..\CCApi.SourceGenerator\CCApi.SourceGenerator.csproj",
                ("Dappi.SourceGenerator", [("Version", tagName), ("OutputItemType", "Analyzer")])
            }
        };
}