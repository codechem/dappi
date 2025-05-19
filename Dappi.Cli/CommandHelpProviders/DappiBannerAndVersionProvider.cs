using System.Collections.Generic;
using System.Globalization;
using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Cli.Help;
using Spectre.Console.Rendering;

namespace Dappi.Cli;

internal class DappiBannerAndVersionProvider(ICommandAppSettings settings) : HelpProvider(settings)
{
    public override IEnumerable<IRenderable> GetHeader(ICommandModel model, ICommandInfo? command)
    {
        return
        [
            Text.NewLine,
            new FigletText("  Dappi").Color(Color.LightGreen_1), Text.NewLine,
            new Text("    Dotnet API Pre-Programming Interface"), Text.NewLine,
            new Text($"    Version: {typeof(Program).Assembly.GetName().Version}"),
            Text.NewLine,
            Text.NewLine,
        ];
    }
}