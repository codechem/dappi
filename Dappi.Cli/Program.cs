using System;
using Dappi.Cli.Commands;
using static Dappi.Cli.Constants;
using Microsoft.Extensions.Hosting;

await Host
    .CreateDefaultBuilder()
    .RunCommandLineApplicationAsync<MainCommand>(args, app =>
    {
        app.Name = CliCommandName;
        app.Description = $"A Dotnet API Pre-Programming Interface";
        app.FullName = DappiBanner + $"{Environment.NewLine}{CliName}";
    });