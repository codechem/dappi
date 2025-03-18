using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Text;

namespace CCApi.SourceGenerator.Generators.ModelGenerators
{
    [Generator]
    public class UpdateAppDbContextControllerGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            context.RegisterImplementationSourceOutput(context.CompilationProvider, (context, compilation) => GenerateController(context, compilation));
        }

        private void GenerateController(SourceProductionContext context, Compilation compilation)
        {
            var rootNamespace = compilation.AssemblyName ?? "DefaultNamespace";

            var sourceText = SourceText.From(
$@"using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.IO;
using System.Linq;

namespace {rootNamespace}.Controllers
{{
[ApiExplorerSettings(GroupName = ""Toolkit"")]
    [Route(""api/update-db-context"")]
    [ApiController]
    public class UpdateAppDbContextController : ControllerBase
    {{
        private readonly string _entitiesFolderPath = Path.Combine(Directory.GetCurrentDirectory(), ""Entities"");
        private readonly string _appDbContextFilePath = Path.Combine(Directory.GetCurrentDirectory(), ""Data"", ""AppDbContext.cs"");

        [HttpGet]
        public IActionResult UpdateDbContext()
        {{
            try
            {{
                var newModels = GetNewModels();

                if (!newModels.Any())
                {{
                    return Ok(""No new models found to add to AppDbContext."");
                }}

                var appDbContextCode = System.IO.File.ReadAllText(_appDbContextFilePath);

                var updatedCode = AddDbSetsToAppDbContextClass(appDbContextCode, newModels);

                System.IO.File.WriteAllText(_appDbContextFilePath, updatedCode);

                return Ok(new
                {{
                    Message = ""AppDbContext updated with new DbSets."",
                    NewModels = newModels
                }});
            }}
            catch (Exception ex)
            {{
                return StatusCode(500, $""Internal server error: {{ex.Message}}"");
            }}
        }}

        private string[] GetNewModels()
        {{
            var modelNames = Directory.GetFiles(_entitiesFolderPath, ""*.cs"")
                                      .Select(Path.GetFileNameWithoutExtension)
                                      .ToList();

            var existingCode = System.IO.File.ReadAllText(_appDbContextFilePath);

            return modelNames.Where(name => !existingCode.Contains($""DbSet<{{name}}>"")).ToArray();
        }}

        private string AddDbSetsToAppDbContextClass(string appDbContextCode, string[] missingModels)
        {{
            var sb = new StringBuilder(appDbContextCode);

            var classEndIndex = appDbContextCode.LastIndexOf(""}}"", StringComparison.Ordinal);

            sb.Insert(classEndIndex, Environment.NewLine);

            foreach (var model in missingModels)
            {{
                sb.Insert(classEndIndex, $""    public DbSet<{{model}}> {{model}}s {{{{ get; set; }}}}{{Environment.NewLine}}"");
            }}

            return sb.ToString();
        }}
    }}
}}
", Encoding.UTF8);
            context.AddSource("UpdateAppDbContextController.cs", sourceText);
        }
    }
}
