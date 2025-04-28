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
        if (string.IsNullOrEmpty(ProjectPath))
        {
            app.ShowHelp();
        }
    }
}