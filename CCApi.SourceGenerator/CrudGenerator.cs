using System.Collections.Immutable;
using System.Text;
using CCApi.SourceGenerator.Extensions;
using CCApi.SourceGenerator.Generators;
using CCApi.SourceGenerator.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static CCApi.SourceGenerator.Utilities.ClassPropertiesAnalyzer;

namespace CCApi.SourceGenerator;

[Generator]
public class CrudGenerator : BaseSourceModelToSourceOutputGenerator
{
    protected override void Execute(SourceProductionContext context, (Compilation Compilation, ImmutableArray<SourceModel> CollectedData) input)
    {
        var (compilation, collectedData) = input;
        var dbContextData = compilation.GetDbContextInformation();
        if (dbContextData is null)
            throw new NullReferenceException("DbContext data is null");
        
        foreach (var item in collectedData)
        {
            var collectionAddCode = GenerateCollectionAddCode(item);
            var generatedCode = $@"
using Microsoft.AspNetCore.Mvc;
using {dbContextData.ResidingNamespace};
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
public partial class {item.ClassName}Controller({dbContextData.ClassName} dbContext) : ControllerBase
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
            .Where(ContainsCollectionTypeName)
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
        
        if ({propNameLower}Ids != null && {propNameLower}Ids.Count > 0)
        {{
            // Fetch existing entities from database instead of creating new ones
            var existing{entityType}s = await dbContext.{entityType}s
                .Where(e => {propNameLower}Ids.Contains(e.Id))
                .ToListAsync();

            if (existing{entityType}s.Any())
            {{
                modelToSave.{prop.PropertyName} = existing{entityType}s;
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
    
}