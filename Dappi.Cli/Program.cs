using Dappi.Cli;
using Dappi.Cli.CommandHelpProviders;
using Dappi.Cli.Commands;
using Dappi.Cli.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Spectre.Console.Cli;

var serviceCollection = new ServiceCollection()
    .AddLogging(configure =>
        configure.AddSerilog(new LoggerConfiguration()
            .MinimumLevel.ControlledBy(LogInterceptor.LogLevel)
            .WriteTo.Console()
            .CreateLogger()
        )
    );

var registrar = new TypeRegistrar(serviceCollection);
var app = new CommandApp(registrar);

app.Configure(cfg =>
{
    cfg.SetHelpProvider(new DappiBannerAndVersionProvider(cfg.Settings));
    cfg.SetApplicationName(Constants.CliCommandName);
    cfg.SetInterceptor(new LogInterceptor());

    cfg.AddCommand<StartCommand>(StartCommand.CommandName);
    cfg.AddCommand<InitCommand>(InitCommand.CommandName);
});

return app.Run(args);