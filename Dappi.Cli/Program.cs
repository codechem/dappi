using System;
using System.Threading.Tasks;
using Dappi.Cli.Commands;
using Microsoft.Extensions.Hosting;

namespace Dappi.Cli;

public static class Program
{
    private const string CliCommandName = "dappi";

    private const string CliName = "Dappi CLI";

    private const string DappiBanner = @"
           ____                    _ 
          |  _ \  __ _ _ __  _ __ (_)
          | | | |/ _` | '_ \| '_ \| |
          | |_| | (_| | |_) | |_) | |
          |____/ \__,_| .__/| .__/|_|
                      |_|   |_|      
        ";

    public static async Task Main(string[] args)
    {
        try
        {
            await Host
                .CreateDefaultBuilder()
                .ConfigureServices((context, services) => { })
                .RunCommandLineApplicationAsync<MainCommand>(args, app =>
                {
                    app.Name = CliCommandName;
                    app.Description = $"A Dotnet API Pre-Programming Interface";
                    app.FullName = DappiBanner + $"{Environment.NewLine}{CliName}";
                });
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }
}