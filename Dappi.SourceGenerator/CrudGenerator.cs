using System.Collections.Immutable;
using System.Text;
using Dappi.SourceGenerator.Extensions;
using Dappi.SourceGenerator.Generators;
using Dappi.SourceGenerator.Models;
using Dappi.Core.Utils;
using Microsoft.CodeAnalysis;
using static Dappi.SourceGenerator.Utilities.ClassPropertiesAnalyzer;

namespace Dappi.SourceGenerator;

[Generator]
public class CrudGenerator : BaseSourceModelToSourceOutputGenerator
{
    protected override void Execute(SourceProductionContext context,
        (Compilation Compilation, ImmutableArray<SourceModel> CollectedData) input)
    {
        var (compilation, collectedData) = input;
        var dbContextData = compilation.GetDbContextInformation();
        if (dbContextData is null)
            throw new NullReferenceException("DbContext data is null");

        var mediaInfoPropertyNames = GetMediaInfoPropertyNames(collectedData);

        foreach (var item in collectedData)
        {
            var collectionAddCode = GenerateCollectionAddCode(item);
            var collectionUpdateCode = GenerateCollectionUpdateCode(item);
            var includesCode = GetIncludesIfAny(item.PropertiesInfos, mediaInfoPropertyNames, item.ClassName);
            var authorizationTags = PropagateDappiAuthorizationTags(item.AuthorizeAttributes, "GET");
            var hasAuthorizationOnControllerLevel = item.AuthorizeAttributes.FirstOrDefault() is { OnControllerLevel: true };
            var authorizeTag = hasAuthorizationOnControllerLevel ? "[Authorize]" : null; 
            var mediaInfoUpdateCode = string.Empty;
            if (mediaInfoPropertyNames.ContainsKey(item.ClassName))
            {
                mediaInfoUpdateCode = GenerateMediaInfoCreationCode("model", "existingModel", mediaInfoPropertyNames[item.ClassName]);
            }
            (string includeCode , string removeCode) = GenerateDeleteCodeForMediaInfo(item);
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
using System.Collections;

/*
==== area for testing ====
{PrintPropertyInfos(item.PropertiesInfos)}
{PrintDappiAuthorizeInfos(item.AuthorizeAttributes)}
==== area for testing ====
*/

namespace {item.RootNamespace}.Controllers;

{authorizeTag}
[ApiController]
[Route(""api/[controller]"")]
public partial class {item.ClassName}Controller(
    {dbContextData.ClassName} dbContext, 
    IMediaUploadService uploadService) : ControllerBase
{{
    [HttpGet]
    {PropagateDappiAuthorizationTags(item.AuthorizeAttributes, "GET")}
    public async Task<IActionResult> Get{item.ClassName.Pluralize()}([FromQuery] {item.ClassName}Filter? filter)
    {{
        var query = dbContext.{item.ClassName.Pluralize()}.AsNoTracking().AsQueryable();
       
        query = query{includesCode};

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

        var query = dbContext.{item.ClassName.Pluralize()}.AsNoTracking().AsQueryable();
       
        query = query{includesCode};

        var result = await query
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
{mediaInfoUpdateCode}

        dbContext.Entry(existingModel).CurrentValues.SetValues(model);

        await dbContext.SaveChangesAsync();
        return Ok(existingModel);
    }}

    [HttpPatch(""{{id}}"")]
    public async Task<IActionResult> JsonPatch{item.ClassName}(Guid id, JsonDocument patchOperations)
    {{
        if (patchOperations is null || id == Guid.Empty)
            return BadRequest(""Invalid data provided."");

        var entity = await dbContext.{item.ClassName.Pluralize()}{includesCode}.FirstOrDefaultAsync(s => s.Id == id);
        
        if (entity is null)
        {{
            return BadRequest(""{item.ClassName} with this id not found."");
        }}
        
        if (patchOperations.RootElement.ValueKind == JsonValueKind.Array)
        {{
            foreach (var patchOperation in patchOperations.RootElement.EnumerateArray())
            {{
                patchOperation.TryGetProperty(""op"", out var operation);
                patchOperation.TryGetProperty(""path"", out var path);
                patchOperation.TryGetProperty(""value"", out var value);
                if (operation.ValueKind == JsonValueKind.String)
                {{
                    var propertyPathValue = path.GetString();
                    var propertyPath = propertyPathValue[0] == '/' ? propertyPathValue.Substring(1, propertyPathValue.Length - 1) : propertyPathValue;
                    var (propertyEntity, property) = GetEntityProperty(entity, propertyPath);
                    switch (operation.GetString())
                    {{
                        case ""add"":
                            if (property.PropertyType.IsGenericType &&
                                property.PropertyType.GetInterfaces().Contains(typeof(ICollection)) || entity.GetType().GetInterfaces().Contains(typeof(IEnumerable)))
                            {{
                                dynamic propertyList = property.GetValue(propertyEntity);
                                dynamic deserializedValue = value.Deserialize(property.PropertyType.GetGenericArguments()[0]);
                                propertyList?.Add(deserializedValue);
                                property.SetValue(propertyEntity, propertyList);
                            }}
                            else
                            {{
                                SetValueToProperty(propertyEntity, property, value);
                            }}
                            break;
                        case ""replace"":
                            SetValueToProperty(propertyEntity, property, value);
                            break;
                        case ""remove"":
                            if (propertyEntity.GetType().IsGenericType &&
                                propertyEntity.GetType().GetInterfaces().Contains(typeof(ICollection)) || entity.GetType().GetInterfaces().Contains(typeof(IEnumerable)))
                            {{
                                var propertyList = propertyEntity as IList;
                                var itemIndex = propertyPath.Substring(propertyPath.LastIndexOf(""/"",
                                    StringComparison.InvariantCultureIgnoreCase) + 1, 1);
                                if (int.TryParse(itemIndex, out int index))
                                {{
                                    var arrayItem = propertyList[index];
                                    propertyList?.RemoveAt(index);
                                    dbContext.Remove(arrayItem);
                                }}
                            }}
                            else
                                SetValueToProperty(propertyEntity, property, null);
                            break;
                        case ""test"":
                            var result = property.GetValue(propertyEntity).Equals(value.Deserialize(property.PropertyType));
                            if (result)                            
                                return Ok(result);
                            return BadRequest(result);
                        case ""copy"":
                            patchOperation.TryGetProperty(""from"", out var from);
                            if (path.ValueKind == JsonValueKind.String && from.ValueKind == JsonValueKind.String)
                            {{
                                var sourcePath = from.GetString();
                                var destinationPath = propertyPath;
                                if (string.IsNullOrEmpty(sourcePath) && string.IsNullOrEmpty(destinationPath))
                                {{
                                    return BadRequest(""Invalid data provided."");
                                }}
                                var sourcePropertyPath = sourcePath[0] == '/' ? sourcePath.Substring(1, sourcePath.Length - 1) : sourcePath;
                                var (sourceEntity, sourceProperty) = GetEntityProperty(entity, sourcePropertyPath);
                                property.SetValue(propertyEntity, sourceProperty.GetValue(sourceEntity));
                            }}
                            break;
                    }}
                }}
            }}
        }}
        await dbContext.SaveChangesAsync();
        return Ok(entity);
    }}

    [HttpDelete(""{{id}}"")]
    {PropagateDappiAuthorizationTags(item.AuthorizeAttributes, "DELETE")}
    public async Task<IActionResult> Delete(Guid id)
    {{
        {includeCode}

        if (model is null)
            return NotFound();

        dbContext.{item.ClassName.Pluralize()}.Remove(model);
        {removeCode}

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

    private static (object entity, PropertyInfo property) GetEntityProperty(object entity, string propertyName)
    {{
        if (entity is null || string.IsNullOrEmpty(propertyName)) throw new ArgumentNullException(nameof(entity));
        if (!propertyName.Contains(""/""))
        {{
            var property = entity.GetType().GetProperty(propertyName);
            return (entity, property);
        }}
        
        var nestedProperties = propertyName.Split('/');
        var nestedPropertyName = nestedProperties[0];
        if (int.TryParse(nestedPropertyName, out int index))
        {{
            if (entity.GetType().GetInterfaces().Contains(typeof(ICollection)) || entity.GetType().GetInterfaces().Contains(typeof(IEnumerable)))
            {{
                dynamic array = entity;
                if (entity.GetType().GetInterfaces().Contains(typeof(IList)))
                {{
                    var arrayElement = array?[index];
                    return GetEntityProperty(arrayElement, string.Join('/', nestedProperties.Skip(1)));
                }}
                else if (entity.GetType().GetInterfaces().Contains(typeof(ISet)))
                {{
                    var hashList = array.ToList();
                    var arrayElement = hashList?[index];
                    return GetEntityProperty(arrayElement, string.Join('/', nestedProperties.Skip(1)));
                }}
        }}
        var nestedEntity = entity.GetType().GetProperty(nestedPropertyName)?.GetValue(entity);
        return GetEntityProperty(nestedEntity, string.Join('/', nestedProperties.Skip(1)));
    }}

    private static void SetValueToProperty(object entity, PropertyInfo property, JsonElement? value)
    {{
        property.SetValue(entity, value?.Deserialize(property.PropertyType));
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

    private static Dictionary<string, IEnumerable<string>> GetMediaInfoPropertyNames(
        ImmutableArray<SourceModel> collectedData)
    {
        return collectedData
            .SelectMany(s => s.PropertiesInfos
                .Where(p => p.PropertyType.Name.Contains("MediaInfo"))
                .Select(p => new { s.ClassName, p.PropertyName }))
            .GroupBy(item => item.ClassName)
            .ToDictionary(g => g.Key, g => g.Select(item => item.PropertyName));
    }

    private static string GenerateMediaInfoCreationCode(string updatedModelName, string existingModelName,
        IEnumerable<string> mediaInfoPropertyNames)
    {
        if (!mediaInfoPropertyNames.Any())
        {
            return string.Empty;
        }
        var sb = new StringBuilder();
        foreach (var prop in mediaInfoPropertyNames)
        {
            sb.AppendLine($@"
        if ({updatedModelName}.{prop} != null) {{
            var existing{prop} = await dbContext.Set<MediaInfo>().FirstOrDefaultAsync(media => media.Url.Equals({updatedModelName}.{prop}.Url) && media.FileSize == {updatedModelName}.{prop}.FileSize);
            if (existing{prop} == null) {{
                var mediaInfo = new MediaInfo
                {{
                   Url = {updatedModelName}.{prop}.Url,
                   OriginalFileName = {updatedModelName}.{prop}.OriginalFileName,
                   FileSize = {updatedModelName}.{prop}.FileSize,
                   UploadDate = {updatedModelName}.{prop}.UploadDate
                }};
                await dbContext.Set<MediaInfo>().AddAsync(mediaInfo);
                {existingModelName}.{prop} = mediaInfo;
            }}
            else {{
                {existingModelName}.{prop} = existing{prop};
            }}
        }}");
        }

        return sb.ToString().TrimEnd();
    }
    
    private static (string include, string remove) GenerateDeleteCodeForMediaInfo(SourceModel model)
    {

        var includeCode = new StringBuilder($"var model = dbContext.{model.ClassName.Pluralize()} \n");
        var removeCode = new StringBuilder();
        removeCode.AppendLine("");
        var mediaInfos = model.PropertiesInfos.Where(p => p.PropertyType.Name.Contains("MediaInfo")).ToList();
        if (mediaInfos.Any())
        {
            foreach (var mediaInfo in mediaInfos)
            {
                removeCode.AppendLine($$"""         if(model.{{mediaInfo.PropertyName}} is not null){ """);
                includeCode.AppendLine($@"                  .Include(p => p.{mediaInfo.PropertyName})");
                removeCode.AppendLine($@"
            dbContext.Set<MediaInfo>().Attach(model.{mediaInfo.PropertyName}); 
            dbContext.Set<MediaInfo>().Remove(model.{mediaInfo.PropertyName});
            uploadService.DeleteMedia(model.{mediaInfo.PropertyName});");
                removeCode.AppendLine("         }");
                removeCode.AppendLine("");
            }
        }

        includeCode.AppendLine("                  .FirstOrDefault(p => p.Id == id);");
        return (includeCode.ToString().TrimEnd(), removeCode.ToString().TrimEnd());
    }
}