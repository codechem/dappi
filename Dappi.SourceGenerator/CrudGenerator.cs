using System.Collections.Immutable;
using System.Text;
using Dappi.SourceGenerator.Extensions;
using Dappi.SourceGenerator.Generators;
using Dappi.SourceGenerator.Models;
using Microsoft.CodeAnalysis;
using static Dappi.SourceGenerator.Utilities.ClassPropertiesAnalyzer;

namespace Dappi.SourceGenerator;

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
            var mediaInfoIncludeCode = GenerateMediaInfoOrRelationIncludeCode(item);
            var collectionUpdateCode = GenerateCollectionUpdateCode(item);
            var includesCode = GetIncludesIfAny(item.PropertiesInfos);
            var authorizationTags = PropagateDappiAuthorizationTags(item.AuthorizeAttributes, "GET");
            // TODO: Change to new project names
            var generatedCode = $@"using Microsoft.AspNetCore.Mvc;
using {dbContextData.ResidingNamespace};
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Nodes;
using Dappi.HeadlessCms.Models;
using Dappi.HeadlessCms.Interfaces;
using {item.ModelNamespace};
using {item.RootNamespace}.Filtering;
using {item.RootNamespace}.HelperDtos;
using {item.RootNamespace}.Extensions;
using Microsoft.AspNetCore.Authorization;
using System.IO;
using System.Reflection;

/*
==== area for testing ====
{PrintPropertyInfos(item.PropertiesInfos)}
{PrintDappiAuthorizeInfos(item.AuthorizeAttributes)}
==== area for testing ====
*/

namespace {item.RootNamespace}.Controllers;

[ApiController]
[Route(""api/[controller]"")]
public partial class {item.ClassName}Controller(
    {dbContextData.ClassName} dbContext, 
    IMediaUploadService uploadService) : ControllerBase
{{
    [HttpGet]
    {PropagateDappiAuthorizationTags(item.AuthorizeAttributes, "GET")}
    public async Task<IActionResult> Get{item.ClassName}([FromQuery] {item.ClassName}Filter? filter)
    {{
{mediaInfoIncludeCode}

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
    {PropagateDappiAuthorizationTags(item.AuthorizeAttributes, "GETBYID")}
    public async Task<IActionResult> Get{item.ClassName}(Guid id)
    {{
        if (id == Guid.Empty)
            return BadRequest();

{mediaInfoIncludeCode}

        var result = await dbContext.{item.ClassName.Pluralize()}{includesCode}
            .FirstOrDefaultAsync(p => p.Id == id);

        if (result is null)
            return NotFound();

        return Ok(result);
    }}

    [HttpPost]
    {PropagateDappiAuthorizationTags(item.AuthorizeAttributes, "POST")}
    public async Task<IActionResult> Create([FromBody] {item.ClassName} model)
    {{
        if (model is null)
            return BadRequest();

        var modelToSave = new {item.ClassName}();
        modelToSave = model;

{collectionAddCode}

        await dbContext.{item.ClassName.Pluralize()}.AddAsync(modelToSave);
        await dbContext.SaveChangesAsync();

        return CreatedAtAction(nameof(Create), new {{ id = modelToSave.Id }}, modelToSave);
    }}

    [HttpPut(""{{id}}"")]
    {PropagateDappiAuthorizationTags(item.AuthorizeAttributes, "PUT")}
    public async Task<IActionResult> Update(Guid id, [FromBody] {item.ClassName} model)
    {{
        if (model == null || id == Guid.Empty)
            return BadRequest(""Invalid data provided."");

        var existingModel = await dbContext.{item.ClassName.Pluralize()}{includesCode}
            .FirstOrDefaultAsync(p => p.Id == id);
            
        if (existingModel == null)
            return NotFound($""{item.ClassName} with ID {{id}} not found."");

        model.Id = id;

{collectionUpdateCode}

        dbContext.Entry(existingModel).CurrentValues.SetValues(model);

        await dbContext.SaveChangesAsync();
        return Ok(existingModel);
    }}

    [HttpDelete(""{{id}}"")]
    {PropagateDappiAuthorizationTags(item.AuthorizeAttributes, "DELETE")}
    public async Task<IActionResult> Delete(Guid id)
    {{
        var model = dbContext.{item.ClassName.Pluralize()}.FirstOrDefault(p => p.Id == id);
        if (model is null)
            return NotFound();

        dbContext.{item.ClassName.Pluralize()}.Remove(model);
        await dbContext.SaveChangesAsync();

        return Ok();
    }}

    [HttpPost(""upload-file/{{id}}"")]
    {PropagateDappiAuthorizationTags(item.AuthorizeAttributes, "POST")}
    public async Task<IActionResult> UploadFile(Guid id, IFormFile file, [FromForm] string fieldName)
    {{
        if (string.IsNullOrEmpty(fieldName))
            return BadRequest(""Field name is required."");

        try
        {{
            var entity = await dbContext.{item.ClassName.Pluralize()}.FindAsync(id);

            if (entity == null)
                return NotFound($""{item.ClassName} with ID {{id}} not found."");

            var property = typeof({item.ClassName}).GetProperty(fieldName);
            if (property == null)
                return BadRequest($""Property {{fieldName}} does not exist."");

            if (property.PropertyType != typeof(MediaInfo))
                return BadRequest($""Property {{fieldName}} must be a MediaInfo type to store media information."");

            var mediaInfo = await uploadService.UploadMediaAsync(id, file);
            property.SetValue(entity, mediaInfo);

            await dbContext.Set<MediaInfo>().AddAsync(mediaInfo);
            await dbContext.SaveChangesAsync();

            dbContext.Entry(entity).State = EntityState.Modified;
            await dbContext.SaveChangesAsync();

            return Ok(mediaInfo);
        }}
        catch (Exception ex)
        {{
            return BadRequest(new {{ message = ex.Message }});
        }}
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
}}";

            context.AddSource($"{item.ClassName}Controller.cs", generatedCode);
        }
    }

    private static string GenerateMediaInfoOrRelationIncludeCode(SourceModel model)
    {
        var includesCode = GetIncludesIfAny(model.PropertiesInfos);

        return $@"        var mediaInfoProperties = typeof({model.ClassName}).GetProperties()
            .Where(p => p.PropertyType == typeof(MediaInfo))
            .ToList();
            
        var query = dbContext.{model.ClassName.Pluralize()}.AsQueryable();
       
        query = query{includesCode};
        
        foreach (var prop in mediaInfoProperties)
        {{
            query = query.Include(prop.Name);
        }}";
    }

    private static string GenerateCollectionUpdateCode(SourceModel model)
    {
        var collectionProperties = model.PropertiesInfos
            .Where(ContainsCollectionTypeName)
            .ToList();

        if (!collectionProperties.Any())
            return string.Empty;

        var sb = new StringBuilder();

        foreach (var prop in collectionProperties)
        {
            var entityType = prop.GenericTypeName;

            if (entityType.EndsWith("?"))
                entityType = entityType.Substring(0, entityType.Length - 1);

            if (entityType.Contains('.'))
                entityType = entityType.Substring(entityType.LastIndexOf('.') + 1);

            var propNameLower = prop.PropertyName.ToLower();

            sb.AppendLine($@"        // Update {prop.PropertyName} collection
        if (model.{prop.PropertyName} != null)
        {{
            var {propNameLower}Ids = model.{prop.PropertyName}.Select(m => m.Id).ToList();
            
            var existing{entityType.Pluralize()} = await dbContext.{entityType.Pluralize()}
                .Where(e => {propNameLower}Ids.Contains(e.Id))
                .ToListAsync();
            
            existingModel.{prop.PropertyName}.Clear();
            existingModel.{prop.PropertyName} = existing{entityType.Pluralize()};
        }}");
        }

        return sb.ToString().TrimEnd();
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

            sb.AppendLine($@"        var {propNameLower}Ids = model.{prop.PropertyName}?.Select(m => m.Id).ToList();
        
        if ({propNameLower}Ids != null && {propNameLower}Ids.Count > 0)
        {{
            var existing{entityType.Pluralize()} = await dbContext.{entityType.Pluralize()}
                .Where(e => {propNameLower}Ids.Contains(e.Id))
                .ToListAsync();

            if (existing{entityType.Pluralize()}.Any())
            {{
                modelToSave.{prop.PropertyName} = existing{entityType.Pluralize()};
            }}
        }}");
        }

        return sb.ToString().TrimEnd();
    }

    private static bool ContainsCollectionTypeName(PropertyInfo x)
    {
        return x.PropertyType.Name.Contains("IEnumerable")
        || x.PropertyType.Name.Contains("List")
        || x.PropertyType.Name.Contains("ICollection");
    }
}