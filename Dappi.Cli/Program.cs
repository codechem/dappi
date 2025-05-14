using System;
using System.Threading.Tasks;
using Dappi.Cli.Commands;
using Microsoft.Extensions.Hosting;

namespace Dappi.Cli;

public static class Program
{
    public static async Task Main(string[] args)
    {
        try
        {
            await Host
                .CreateDefaultBuilder()
                .RunCommandLineApplicationAsync<MainCommand>(args, app =>
                {
                    app.Name = Constants.CliCommandName;
                    app.Description = $"A Dotnet API Pre-Programming Interface";
                    app.FullName = Constants.DappiBanner + $"{Environment.NewLine}{Constants.CliName}";
                });
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }
}