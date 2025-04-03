using System.Collections.Immutable;
using System.Text;
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
            predicate: (node, _) => node is ClassDeclarationSyntax,
            transform: (ctx, _) => 
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
            var collectionAddCode = GenerateCollectionAddCode(item);
            var generatedCode = $@"
using Microsoft.AspNetCore.Mvc;
using CCApi.WebApiExample.Data; //TODO: AppDbContext here
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Nodes;
using {item.ModelNamespace};

using {item.RootNamespace}.Filtering;
using {item.RootNamespace}.HelperDtos;
using {item.RootNamespace}.Extensions;

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
     public async Task<IActionResult> Get{item.ClassName}s([FromQuery] {item.ClassName}Filter? filter)
     {{
         var query = dbContext.{item.ClassName}s{GetIncludesIfAny(item.PropertiesInfos)}.AsQueryable();

         if (filter != null)
         {{
            query = LinqExtensions.ApplyFiltering(query, filter);
         }}

         if (!string.IsNullOrEmpty(filter.SortBy))
         {{
            query = LinqExtensions.ApplySorting(query, filter.SortBy, filter.SortDirection);
         }}

         var total = await query.CountAsync();
         var data = await query
             .Skip(filter.Offset)
             .Take(filter.Limit)
             .ToListAsync();

         var listDto = new ListResponseDTO<{item.ClassName}>
         {{
             Data = data,
             Limit = filter.Limit,
             Offset = filter.Offset,
             Total = total
         }};

        return Ok(listDto);
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
    public async Task<IActionResult> Create([FromBody] {item.ClassName} model)
    {{
        if(model is null) 
            return BadRequest();
        
        var modelToSave = new {item.ClassName}();
        modelToSave = model;

        {collectionAddCode}

        await dbContext.{item.ClassName}s.AddAsync(modelToSave);
        await dbContext.SaveChangesAsync();

        return CreatedAtAction(nameof(Create), new {{ id = modelToSave.Id }}, modelToSave);
    }}

     [HttpPut(""{{id}}"")]
    public async Task<IActionResult> Update(Guid id, [FromBody] {item.ClassName} model)
     {{
         if (model == null || id == Guid.Empty)
            return BadRequest(""Invalid data provided."");

         var existingModel = await dbContext.{item.ClassName}s.FirstOrDefaultAsync(p => p.Id == id);
         if (existingModel == null)
            return NotFound($""{item.ClassName}s with ID {{id}} not found."");

         model.Id = id;
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

     private dynamic GetDbSetForType(string typeName)
     {{
        var dbSetProperty = dbContext.GetType()
            .GetProperties()
            .FirstOrDefault(p => 
                p.PropertyType.IsGenericType && 
                p.PropertyType.GetGenericArguments()[0].Name.Equals(typeName, StringComparison.OrdinalIgnoreCase));

        return dbSetProperty?.GetValue(dbContext);
    }}
}}
";
            context.AddSource($"{item.ClassName}Controller.cs", generatedCode);
        }
    }

    private static string GenerateCollectionAddCode(SourceModel model)
    {
        var collectionProperties = model.PropertiesInfos
            .Where(x => ContainsCollectionTypeName(x))
            .ToList();

        if (!collectionProperties.Any())
            return string.Empty;

        var sb = new StringBuilder();
        foreach (var prop in collectionProperties)
        {
            string entityType = prop.GenericTypeName;

            if (entityType.EndsWith("?"))
                entityType = entityType.Substring(0, entityType.Length - 1);

            if (entityType.Contains('.'))
                entityType = entityType.Substring(entityType.LastIndexOf('.') + 1);

            string propNameLower = prop.PropertyName.ToLower();

            sb.AppendLine($@"var {propNameLower}Ids = model.{prop.PropertyName}?.Select(m => m.Id).ToList();
        modelToSave.{prop.PropertyName} = new List<{entityType}>();

        if ({propNameLower}Ids != null && {propNameLower}Ids.Count > 0)
        {{
            foreach (var {entityType.ToLower()}Id in {propNameLower}Ids)
            {{
                var {entityType.ToLower()} = new {entityType} {{ Id = {entityType.ToLower()}Id }};
                
                dbContext.Attach({entityType.ToLower()});
                
                modelToSave.{prop.PropertyName}.ToList().Add({entityType.ToLower()});
            }}
        }}");
        }

        return sb.ToString();
    }

    private static bool ContainsCollectionTypeName(PropertyInfo x)
    {
        return x.PropertyType.Name.Contains("IEnumerable")
        || x.PropertyType.Name.Contains("List")
        || x.PropertyType.Name.Contains("ICollection");
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