using System.Collections.Immutable;
using CCApi.SourceGenerator.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static CCApi.SourceGenerator.Utilities.ClassPropertiesAnalyzer;

namespace CCApi.SourceGenerator;

[Generator]
public class CrudGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var syntaxProvider = context.SyntaxProvider.ForAttributeWithMetadataName(
            "CCApi.SourceGenerator.Attributes.CCControllerAttribute",
            (node, _) => node is ClassDeclarationSyntax,
            (ctx, _) => 
            {
                var classDeclaration = (ClassDeclarationSyntax)ctx.TargetNode;
                var classSymbol = (ISymbol)ctx.TargetSymbol;
                var namedClassTypeSymbol = (INamedTypeSymbol)ctx.TargetSymbol;
                var attributeData = ctx.Attributes.FirstOrDefault();
                
                return new SourceModel
                {
                    ClassName = classDeclaration.Identifier.Text,
                    ModelNamespace = classSymbol.ContainingNamespace.ToString() ?? string.Empty,
                    RootNamespace = GetRootNamespace(classSymbol.ContainingNamespace),
                    PropertiesInfos = GoThroughPropertiesAndGatherInfo(namedClassTypeSymbol)
                };
            });
        
        var compilation = context.CompilationProvider.Combine(syntaxProvider.Collect());
        context.RegisterSourceOutput(compilation, Execute);
    }

    private static void Execute(SourceProductionContext context, (Compilation Compilation, ImmutableArray<SourceModel> CollectedData) input)
    {
        var (compilation, collectedData) = input;

        foreach (var item in collectedData)
        {
            var generatedCode = $@"
using Microsoft.AspNetCore.Mvc;
using CCApi.WebApiExample.Data; //TODO: AppDbContext here
using Microsoft.EntityFrameworkCore;
using {item.ModelNamespace};

/*
==== area for testing ====
{PrintPropertyInfos(item.PropertiesInfos)}
==== area for testing ====
*/

namespace {item.RootNamespace}.Controllers;

[ApiController]
[Route(""api/[controller]"")]
public partial class {item.ClassName}Controller(AppDbContext dbContext) : ControllerBase
{{
     [HttpGet]
     public async Task<IActionResult> Get{item.ClassName}s()
     {{
         var result = await dbContext.{item.ClassName}s
                        {GetIncludesIfAny(item.PropertiesInfos)}
                        .ToListAsync();
         return Ok(result);
     }}

     [HttpGet(""{{id}}"")]
     public async Task<IActionResult> Get{item.ClassName}(Guid id)
     {{
         if(id == Guid.Empty)
            return BadRequest();
        
         var result = await dbContext.{item.ClassName}s
                        {GetIncludesIfAny(item.PropertiesInfos)}
                        .FirstOrDefaultAsync(p => p.Id == id);
         if (result is null)
            return NotFound();

         // transform to DTO before return
         return Ok(result);
     }}

     [HttpPost]
     public async Task<IActionResult> Create([FromBody] {item.ClassName} model) // TODO: should be DTO
     {{
         if(model is null) 
            return BadRequest();
         await dbContext.{item.ClassName}s.AddAsync(model);
         await dbContext.SaveChangesAsync();

         return Created();
     }}

     [HttpPut(""{{id}}"")]
     public async Task<IActionResult> Update(Guid id, {item.ClassName} model)
     {{
         if (model == null || id == Guid.Empty)
            return BadRequest(""Invalid data provided."");

         var existingModel = await dbContext.{item.ClassName}s.FirstOrDefaultAsync(p => p.Id == id);
         if (existingModel == null)
            return NotFound($""{item.ClassName}s with ID {{id}} not found."");

         // Map incoming model to the existing entity
         model.Id = id; // Ensure the ID remains consistent
         dbContext.Entry(existingModel).CurrentValues.SetValues(model);

         await dbContext.SaveChangesAsync();
         return Ok(existingModel);
    }}

     [HttpDelete(""{{id}}"")]
     public async Task<IActionResult> Delete(Guid id)
     {{
         var model = dbContext.{item.ClassName}s.FirstOrDefault(p => p.Id == id);
         if (model is null)
            return NotFound();

         dbContext.{item.ClassName}s.Remove(model);
         await dbContext.SaveChangesAsync();

         return Ok();
     }}
}}
";
            context.AddSource($"{item.ClassName}Controller.cs", generatedCode);
        }
    }

    private static string GetRootNamespace(INamespaceSymbol namespaceSymbol)
    {
        if (namespaceSymbol is null || namespaceSymbol.IsGlobalNamespace)
        {
            return string.Empty;
        }

        var result = string.Empty;
        var current = namespaceSymbol;
        
        while (current.ContainingNamespace != null)
        {
            if(NamespacesAnotatingAfterRoot.Any(p => p.Equals(current.Name, StringComparison.OrdinalIgnoreCase)))
            {
                current = current.ContainingNamespace;
                continue;
            }

            result = string.Concat(current.ContainingNamespace.Name + ".", result);
            current = current.ContainingNamespace;
        }

        // stupid shit
        return result.Remove(0, 1).Remove(result.Length - 2, 1);
    }

    private static List<string> NamespacesAnotatingAfterRoot = ["Models", "Services", "Controllers"];
}