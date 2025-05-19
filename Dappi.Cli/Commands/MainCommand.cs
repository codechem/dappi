using System.Reflection;
using McMaster.Extensions.CommandLineUtils;

namespace Dappi.Cli.Commands;

[Command]
[VersionOptionFromMember(MemberName = nameof(GetVersion))]
[HelpOption]
[Subcommand(typeof(InitCommand), typeof(StartCommand))]
public class MainCommand
{
    public virtual void OnExecute(CommandLineApplication app)
    {
        app.ShowHelp();
    }

    private static string? GetVersion()
    {
        return typeof(MainCommand)
            .Assembly?.GetName().Version?.ToString();
    }
}