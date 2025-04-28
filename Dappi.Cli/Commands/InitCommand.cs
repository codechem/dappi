using System.IO;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;

namespace Dappi.Cli.Commands;

[Command("init", FullName = "Solution Initializer", Description = "Creates a new Dappi Solution")]
[HelpOption]
public class InitCommand
{
    [Option("-n|--name <NAME>", "The name of your new project.", CommandOptionType.SingleValue)]
    public string ProjectName { get; set; }

    [Option("-p|--path <PATH>", "The path to where your new project should be initialized. Defaults to the current directory.", CommandOptionType.SingleValue)]
    public string ProjectPath { get; set; }
    
    private async Task OnExecute(CommandLineApplication app)
    {
        if (string.IsNullOrEmpty(ProjectName))
        {
            app.ShowHelp();
            return;
        }
        
        var projectPath = string.IsNullOrEmpty(ProjectPath) ? Directory.GetCurrentDirectory() : ProjectPath; 
        var templateFetcher = new TemplateFetcher();
        
        var template = await templateFetcher.GetDappiTemplate();
        var outputFolder = Path.Combine(ProjectName, projectPath);
        var placeholder = "MyCompany.MyProject.WebApi";
        
        ExtractHelper.ExtractZipFile(template, outputFolder, "templates");
        RenameHelper.RenameFolders(outputFolder, placeholder, ProjectName, renameBackup: true, excludedSubFolders: []);
    }
    
   
}