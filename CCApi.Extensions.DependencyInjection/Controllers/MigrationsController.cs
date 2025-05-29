using System.Diagnostics;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;

namespace CCApi.Extensions.DependencyInjection.Controllers;

[ApiExplorerSettings(GroupName = "Toolkit")]
[ApiController]
[Route("api/create-migrations-update-db")]
public class MigrationController : ControllerBase
{
    private readonly IHostApplicationLifetime _appLifetime;
    private readonly string _projectDirectory;

    public MigrationController(IHostApplicationLifetime appLifetime)
    {
        _appLifetime = appLifetime;
        _projectDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location ?? Directory.GetCurrentDirectory()) ?? Directory.GetCurrentDirectory();
    }

    [HttpPost]
    public IActionResult ApplyMigrationsAndRestart()
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                RunDbMigrationScenarioForWindows();
            }
            else
            {
                RunDbMigrationScenario();
            }

            return Ok("Migrations applied. Application restarting...");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error: {ex.Message}");
        }
    }

    private void RunDbMigrationScenario()
    {
        GenerateMigrationsIfNeeded();
        var directory = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location);
        ApplyMigrationsAfterRestart(directory);
        RestartApplication();
        _appLifetime.StopApplication();
    }

    private void RunDbMigrationScenarioForWindows()
    {
        var currentDir = Directory.GetCurrentDirectory();
        var csproj = Directory.GetFiles(currentDir, "*.csproj", SearchOption.TopDirectoryOnly).FirstOrDefault();
        var scriptPath = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "Scripts", "Start-DappiMigrationRunner.ps1");
        var procId = Environment.ProcessId;

        var args = $"-ExecutionPolicy Bypass -File \"{scriptPath}\" -ProjectPath \"{_projectDirectory}\" -Csproj \"{csproj}\" -ProcessId \"{procId}\" -MigrationName \"{GetMigrationName()}\"";
        var psi = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = args,
            UseShellExecute = true,
            CreateNoWindow = true,
        };

        Process.Start(psi);
    }

    private void GenerateMigrationsIfNeeded()
    {
        var migrationDirectory = Path.Combine(_projectDirectory, "Migrations");
        if (!Directory.Exists(migrationDirectory))
        {
            Directory.CreateDirectory(migrationDirectory);
        }

        var startInfo = new ProcessStartInfo
        {
            WorkingDirectory = Directory.GetCurrentDirectory(),
            FileName = "dotnet",
            Arguments = $"ef migrations add {GetMigrationName()}",
            UseShellExecute = false,
            CreateNoWindow = true
        };
        Process.Start(startInfo)?.WaitForExit();
    }

    private void RestartApplication()
    {
        try
        {
            var exePath = Assembly.GetEntryAssembly()!.Location;
            var directory = Path.GetDirectoryName(exePath);
            var processId = Environment.ProcessId;
            var scriptPath = Path.Combine(directory, "Scripts", "restart-app.sh");
            Process.Start("chmod", new[] { "+x", scriptPath })?.WaitForExit();

            var startInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"-c \"{scriptPath}\" {processId} {exePath} --restart",
                UseShellExecute = false,
                CreateNoWindow = true
            };
            startInfo.EnvironmentVariables["DAPPI_MIGRATION_RESTART"] = "true";

            Process.Start(startInfo);
            _appLifetime.StopApplication();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to restart application: {ex.Message}");
        }
    }

    private void ApplyMigrationsAfterRestart(string directory)
    {
        try
        {
            var currentDir = Directory.GetCurrentDirectory();
            var csproj = Directory.GetFiles(currentDir, "*.csproj", SearchOption.TopDirectoryOnly).FirstOrDefault();

            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"ef database update --project {csproj}",
                UseShellExecute = false,
                CreateNoWindow = true
            };
            Process.Start(startInfo)?.WaitForExit();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to apply migrations after restart: {ex.Message}");
        }
    }

    private string GetMigrationName()
    {
        var formattedDate = DateTime.Now.ToString("yyyyMMddHHmmss");
        return $"DappiGeneratedMigration_{formattedDate}";
    }
}