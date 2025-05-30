using System.Text;
using Dappi.SourceGenerator.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Dappi.SourceGenerator.Generators;

[Generator]
public class EnumToolingGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var enumProvider = context.CompilationProvider
            .Select((compilation, _) => GetEnumTypes(compilation));

        context.RegisterImplementationSourceOutput(
            context.CompilationProvider,
            (context, compilation) => GenerateGeneralController(context, compilation)
        );

        context.RegisterImplementationSourceOutput(
            enumProvider,
            (context, enums) => GenerateIndividualControllers(context, enums)
        );
    }

    private static EnumInfo[] GetEnumTypes(Compilation compilation)
    {
        return compilation.GetSymbolsWithName(_ => true, SymbolFilter.Type)
            .OfType<INamedTypeSymbol>()
            .Where(t => t.TypeKind == TypeKind.Enum &&
                       t.ContainingNamespace != null &&
                       t.ContainingNamespace.ToDisplayString().ToLower().Contains("models"))
            .Select(enumSymbol => new EnumInfo
            {
                Name = enumSymbol.Name,
                FullName = enumSymbol.ToDisplayString(),
                Namespace = enumSymbol.ContainingNamespace.ToDisplayString(),
                Members = enumSymbol.GetMembers()
                    .OfType<IFieldSymbol>()
                    .Where(f => f.IsStatic && f.HasConstantValue)
                    .Select(f => new EnumMember
                    {
                        Name = f.Name,
                        Value = Convert.ToInt32(f.ConstantValue)
                    })
                    .ToArray()
            })
            .ToArray();
    }

    private void GenerateGeneralController(SourceProductionContext context, Compilation compilation)
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

    private void GenerateIndividualControllers(SourceProductionContext context, EnumInfo[] enums)
    {
        foreach (var enumInfo in enums)
        {
            var controllerName = $"{enumInfo.Name}Controller";
            var routeName = enumInfo.Name.ToLower();

            var sourceText = SourceText.From($@"
using Microsoft.AspNetCore.Mvc;

namespace {enumInfo.Namespace.Replace(".Models", ".Controllers")};

[ApiExplorerSettings(GroupName = ""Toolkit"")]
[ApiController]
[Route(""api/{routeName}"")]
public class {controllerName} : ControllerBase
{{
    [HttpGet]
    public IActionResult Get()
    {{
        var data = new[]
        {{
{string.Join(",\n", enumInfo.Members.Select(m => $"            new Dictionary<string, int> {{ [\"{m.Name}\"] = {m.Value} }}"))}
        }};

        var response = new
        {{
            Total = data.Length,
            Offset = 0,
            Limit = 0,
            Data = data
        }};

        return Ok(response);
    }}
}}
", Encoding.UTF8);

            context.AddSource($"{controllerName}.cs", sourceText);
        }
    }
}