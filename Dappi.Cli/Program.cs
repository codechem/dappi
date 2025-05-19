using Dappi.Cli;
using Dappi.Cli.Commands;
using Dappi.Cli.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Spectre.Console.Cli;

// to retrieve the log file name, we must first parse the command settings
// this will require us to delay setting the file path for the file writer.
// With serilog we can use an enricher and Serilog.Sinks.Map to dynamically
// pull this setting.
var serviceCollection = new ServiceCollection()
    .AddLogging(configure =>
        configure.AddSerilog(new LoggerConfiguration()
            // log level will be dynamically be controlled by our log interceptor upon running
            .MinimumLevel.ControlledBy(LogInterceptor.LogLevel)
            .WriteTo.Console()
            .CreateLogger()
        )
    );

var registrar = new TypeRegistrar(serviceCollection);
var app = new CommandApp(registrar);

app.Configure(cfg =>
{
    // Register the custom help provider
    cfg.SetHelpProvider(new DappiBannerAndVersionProvider(cfg.Settings));
    
    cfg.SetApplicationName(Constants.CliCommandName);

    cfg.SetInterceptor(new LogInterceptor());

    cfg.AddCommand<StartCommand>(StartCommand.CommandName);
    cfg.AddCommand<InitCommand>(InitCommand.CommandName);
});

return app.Run(args);