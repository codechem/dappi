using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace CCApi.SourceGenerator.Generators;

[Generator]
public class EnumToolingGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterImplementationSourceOutput(
            context.CompilationProvider,
            (context, compilation) => GenerateFiltering(context, compilation)
        );
    }

    private void GenerateFiltering(SourceProductionContext context, Compilation compilation)
    {
        var rootNamespace = compilation.AssemblyName ?? "DefaultNamespace";

        var sourceText = SourceText.From($@"

using System.Reflection;
using Microsoft.AspNetCore.Mvc;

namespace {rootNamespace}.Controllers;

[ApiExplorerSettings(GroupName = ""Toolkit"")]
[ApiController]
[Route(""api/enums"")]
public class EnumToolingController : ControllerBase
{{
    [HttpGet(""getAll"")]
    public IActionResult GetEnums()
    {{
        var enums = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => t.IsEnum && t.Namespace != null && t.Namespace.ToLower().Contains(""models""))
            .ToDictionary(
                enumType => enumType.Name,
                enumType => Enum.GetNames(enumType)
                    .ToDictionary(
                        name => name,
                        name => (int)Enum.Parse(enumType, name)
                    )
            );

        return Ok(enums);
    }}
}}
", Encoding.UTF8);
        context.AddSource("EnumToolingController.cs", sourceText);
    }
}