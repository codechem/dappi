using System.Reflection;
using McMaster.Extensions.CommandLineUtils;

namespace Dappi.Cli.Commands;

[Command]
[VersionOptionFromMember(MemberName = "GetVersion")]
[HelpOption]
[Subcommand(typeof(InitCommand), typeof(StartCommand))]
public class MainCommand
{

    public virtual void OnExecute(CommandLineApplication app)
    {
        app.ShowHelp();
    }

    private string GetVersion()
    {
        return typeof(MainCommand)
            .Assembly?
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;
    }
}