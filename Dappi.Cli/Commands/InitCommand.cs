using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Build.Construction;

namespace Dappi.Cli.Commands;

[Command("init", FullName = "Solution Initializer", Description = "Creates a new Dappi Solution")]
[HelpOption]
public class InitCommand
{
    [Option("-n|--name <NAME>", "The name of your new project.", CommandOptionType.SingleValue)]
    public string? ProjectName { get; set; }

    [Option("-p|--path <PATH>",
        "The path to where your new project should be initialized. Defaults to the current directory.",
        CommandOptionType.SingleValue)]
    public string? ProjectPath { get; set; }

    [Option("--use-prerelease", "Use a pre-release version of Dappi.", CommandOptionType.NoValue)]
    public bool UsePreRelease { get; set; }

    private async Task OnExecute(CommandLineApplication app)
    {
        if (string.IsNullOrEmpty(ProjectName))
        {
            app.ShowHelp();
            return;
        }

        var projectPath = string.IsNullOrEmpty(ProjectPath) ? Directory.GetCurrentDirectory() : ProjectPath;
        var template = await TemplateFetcher.GetDappiTemplate(usePreRelease: UsePreRelease);
        var outputFolder = Path.Combine(projectPath, ProjectName);

        ZipHelper.ExtractZipFile(template.physicalPath, outputFolder, "templates");
        
        RenameHelper.RenameFolders(outputFolder, Constants.ProjectNamePlaceholder, ProjectName, excludedSubFolders: []);

        var csProjFile = Directory.GetFiles(outputFolder, "*.csproj", SearchOption.AllDirectories).FirstOrDefault()!;

        ModifyCsprojWithNugetReferences(template, csProjFile);

        var procStartInfo = new ProcessStartInfo()
        {
            UseShellExecute = false,
            WorkingDirectory = Path.GetDirectoryName(outputFolder),
            FileName = "dotnet",
            Arguments = "ef migrations add Dappi_InitialMigration --project " + csProjFile,
        };  
        
        Console.WriteLine("Generating migrations...");
        var process = Process.Start(procStartInfo);
        process?.WaitForExit();
        
        Console.WriteLine("Dappi initialization complete.");
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