using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Text;

namespace CCApi.SourceGenerator.Generators.ModelGenerators
{
    [Generator]
    public class MigrationControllerGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            context.RegisterImplementationSourceOutput(context.CompilationProvider, (context, compilation) => GenerateController(context, compilation));
        }

        private void GenerateController(SourceProductionContext context, Compilation compilation)
        {
            var rootNamespace = compilation.AssemblyName ?? "DefaultNamespace";

            var sourceText = SourceText.From(
$@"using CCApi.WebApiExample.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Reflection;
using System.IO;

namespace {rootNamespace}.Controllers;

[ApiExplorerSettings(GroupName = ""Toolkit"")]
[ApiController]
[Route(""api/create-migrations-update-db"")]
public class MigrationController : ControllerBase
{{
    private readonly IHostApplicationLifetime _appLifetime;
    private readonly string _projectDirectory;

    public MigrationController(
        AppDbContext context,
        IHostApplicationLifetime appLifetime)
    {{
        _appLifetime = appLifetime;
        _projectDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location);
    }}

    [HttpPost]
    public IActionResult ApplyMigrationsAndRestart()
    {{
        try
        {{
            GenerateMigrationsIfNeeded();
            var directory = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location);
            ApplyMigrationsAfterRestart(directory);
            RestartApplication();
            _appLifetime.StopApplication();
            return Ok(""Migrations applied. Application restarting..."");
        }}
        catch (Exception ex)
        {{
            return StatusCode(500, $""Error: {{ex.Message}}"");
        }}
    }}

    private void GenerateMigrationsIfNeeded()
    {{
        try
        {{
            var migrationDirectory = Path.Combine(_projectDirectory, ""Migrations"");
            if (!Directory.Exists(migrationDirectory))
            {{
                Directory.CreateDirectory(migrationDirectory);
            }}
            string formattedDate = DateTime.Now.ToString(""yyyyMMddHHmmss"");
            ProcessStartInfo startInfo = new ProcessStartInfo
            {{
                FileName = ""dotnet"",
                Arguments = $""ef migrations add AutoGeneratedMigration_{{formattedDate}} --project {{_projectDirectory}}/../../../CCApi.WebApiExample.csproj"",
                UseShellExecute = false,
                CreateNoWindow = true
            }};
            Process.Start(startInfo)?.WaitForExit();
        }}
        catch (Exception ex)
        {{
            Console.WriteLine($""Error creating migrations: {{ex.Message}}"");
        }}
    }}

    private void RestartApplication()
    {{
        try
        {{
            var exePath = Assembly.GetEntryAssembly()?.Location;
            if (exePath != null)
            {{
                var directory = Path.GetDirectoryName(exePath);
                var processId = Process.GetCurrentProcess().Id;
                string scriptPath;
                string scriptContent;
                if (OperatingSystem.IsWindows())
                {{
                    scriptPath = Path.Combine(directory, ""restart.bat"");
                    scriptContent = $@""
@echo off
set pid={{processId}}
set app_path=""""{{exePath}}""""

:waitloop
tasklist /FI """"PID eq %pid%"""" | find /I """"%pid%"""" >nul
if not errorlevel 1 (
    timeout /t 1 >nul
    goto waitloop
)

start """""""" dotnet """"%app_path%""""
exit
"";
                }}
                else
                {{
                    scriptPath = Path.Combine(directory, ""restart.sh"");
                    scriptContent = $@""
#!/bin/bash
pid={{processId}}
app_path=""""{{exePath}}""""

# Wait until the old process exits
while kill -0 """"$pid"""" 2>/dev/null; do sleep 1; done

# Start the new instance
dotnet """"$app_path"""" &
"";
                    System.IO.File.WriteAllText(scriptPath, scriptContent);
                    Process.Start(""chmod"", new[] {{ ""+x"", scriptPath }})?.WaitForExit();
                }}
                System.IO.File.WriteAllText(scriptPath, scriptContent);
                var startInfo = new ProcessStartInfo
                {{
                    FileName = OperatingSystem.IsWindows() ? ""cmd.exe"" : ""/bin/bash"",
                    Arguments = OperatingSystem.IsWindows() ? $""/c \""{{scriptPath}}\"""" : $""-c \""{{scriptPath}}\"""",
                    UseShellExecute = false,
                    CreateNoWindow = true
                }};
                Process.Start(startInfo);
                _appLifetime.StopApplication();
            }}
        }}
        catch (Exception ex)
        {{
            Console.WriteLine($""Failed to restart application: {{ex.Message}}"");
        }}
    }}

    private void ApplyMigrationsAfterRestart(string directory)
    {{
        try
        {{
            ProcessStartInfo startInfo = new ProcessStartInfo
            {{
                FileName = ""dotnet"",
                Arguments = $""ef database update --project {{_projectDirectory}}/../../../CCApi.WebApiExample.csproj"",
                UseShellExecute = false,
                CreateNoWindow = true
            }};
            Process.Start(startInfo)?.WaitForExit();
        }}
        catch (Exception ex)
        {{
            Console.WriteLine($""Failed to apply migrations after restart: {{ex.Message}}"");
        }}
    }}
}}
", Encoding.UTF8);
            context.AddSource("MigrationControllerGenerator.cs", sourceText);
        }
    }
}
