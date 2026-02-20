using System.Collections.Immutable;
using System.Text;
using Dappi.Core.Attributes;
using Dappi.Core.Enums;
using Dappi.Core.Utils;
using Dappi.SourceGenerator.Extensions;
using Dappi.SourceGenerator.Generators;
using Dappi.SourceGenerator.Models;
using Microsoft.CodeAnalysis;
using static Dappi.SourceGenerator.Generators.ActionsGenerator;
using static Dappi.SourceGenerator.Utilities.ClassPropertiesAnalyzer;

namespace Dappi.SourceGenerator;

[Generator]
public class CrudGenerator : BaseSourceModelToSourceOutputGenerator
{
    protected override void Execute(
        SourceProductionContext context,
        (Compilation Compilation, ImmutableArray<SourceModel> CollectedData) input
    )
    {
        var (compilation, collectedData) = input;
        var dbContextData = compilation.GetDbContextInformation();
        if (dbContextData is null)
            throw new NullReferenceException("DbContext data is null");

        var mediaInfoPropertyNames = GetMediaInfoPropertyNames(collectedData);

        foreach (var item in collectedData)
        {
            var arguments = string.Join(
                ",",
                item.CrudActions.Select(x => $"{nameof(CrudActions)}.{x}")
            );
            var controllerAttribute = $"[{CcControllerAttribute.ShortName}({arguments})]";
            var collectionAddCode = GenerateCollectionAddCode(item);
            var collectionUpdateCode = GenerateCollectionUpdateCode(item);
            var includesCode = GetIncludesIfAny(
                item.PropertiesInfos,
                mediaInfoPropertyNames,
                item.ClassName
            );
            var hasAuthorizationOnControllerLevel =
                item.AuthorizeAttributes.FirstOrDefault() is { OnControllerLevel: true };
            var authorizeTag = hasAuthorizationOnControllerLevel ? "[Authorize]" : null;
            var mediaInfoUpdateCode = string.Empty;
            if (mediaInfoPropertyNames.ContainsKey(item.ClassName))
            {
                mediaInfoUpdateCode = GenerateMediaInfoCreationCode(
                    "model",
                    "existingModel",
                    mediaInfoPropertyNames[item.ClassName]
                );
            }

            (string includeCode, string removeCode) = GenerateDeleteCodeForMediaInfo(item);
            var generatedCode =
                $@"using Microsoft.AspNetCore.Mvc;
using {dbContextData.ResidingNamespace};
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Nodes;
using Dappi.HeadlessCms.Models;
using Dappi.HeadlessCms.Interfaces;
using Dappi.HeadlessCms.Extensions;
using Dappi.HeadlessCms.Exceptions;
using Dappi.HeadlessCms.ActionFilters;
using Dappi.HeadlessCms.Core.Requests;
using Dappi.HeadlessCms.Enums;
using Dappi.Core.Attributes;
using Dappi.Core.Enums;
using {item.ModelNamespace};
using {item.RootNamespace}.Filtering;
using {item.RootNamespace}.HelperDtos;
using {item.RootNamespace}.Extensions;
using Microsoft.AspNetCore.Authorization;
using System.IO;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using Dappi.Core.Constants;
using System.Globalization;
using System.Linq;
using System.Linq.Dynamic.Core;
using Dappi.Core.Extensions;

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
[AuthorizeActionsFilter]
{controllerAttribute}
public partial class {item.ClassName}Controller(
    {dbContextData.ClassName} dbContext,
    IMediaUploadService uploadService,
    IMediaUploadQueue queue) : ControllerBase
{{
    {AggregateActions(item, includesCode, collectionAddCode, collectionUpdateCode, mediaInfoUpdateCode, includeCode, removeCode)}    

    private static (object entity, PropertyInfo property, Type[] entityInterfaces, bool isEnumerable, bool isCollection) GetEntityProperty(object entity, string propertyName)
    {{
        if (entity is null || string.IsNullOrEmpty(propertyName)) throw new ArgumentNullException(nameof(entity));
        var entityInterfaces = entity.GetType().GetInterfaces();
        var isEnumerable = entityInterfaces.Contains(typeof(IEnumerable));
        var isCollection = entityInterfaces.Contains(typeof(ICollection));
        if (!propertyName.Contains(""/""))
        {{
            var property = entity.GetType().GetProperty(propertyName);
            return (entity, property, entityInterfaces, isEnumerable, isCollection);
        }}
        
        var nestedProperties = propertyName.Split('/');
        var nestedPropertyName = nestedProperties[0];
        if (int.TryParse(nestedPropertyName, out int index))
        {{
            if (isCollection || isEnumerable)
            {{
                var array = entity as IEnumerable<object>;
                var arrayElement = array.ElementAt(index);
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
    
    private IQueryable<{item.ClassName}> ApplyDynamicIncludes(IQueryable<{item.ClassName}> query)
    {{
        if (!HttpContext.Request.Query.ContainsKey(""include""))
        {{
            return query;
        }}

        var shouldApplyFullIncludes = HttpContext.Request.Query[""include""]
            .SelectMany(includeValue => includeValue
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Any(includePath => includePath == ""*"");

        if (shouldApplyFullIncludes)
        {{
            return ApplyFullIncludes(query);
        }}

        var includeTree = HttpContext.Items[IncludeQueryFilter.IncludeParamsKey] as IDictionary<string, IncludeNode>;
        if (includeTree is null || includeTree.Count == 0)
        {{
            return query;
        }}

        foreach (var include in includeTree)
        {{
            query = ApplyIncludeRecursively(query, include.Key, include.Value);
        }}

        return query;
    }}

private IQueryable<{item.ClassName}> ApplyFullIncludes(IQueryable<{item.ClassName}> query)
    {{
        var rootEntityType = dbContext.Model.FindEntityType(typeof({item.ClassName}));
        if (rootEntityType is null)
        {{
            return query;
        }}

        var includePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var visitedTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        visitedTypes.Add(rootEntityType.Name);
        var prefix = string.Empty;

        CollectIncludePaths(rootEntityType, prefix, includePaths, visitedTypes);

        foreach (var includePath in includePaths)
        {{
            query = query.Include(includePath);
        }}

        return query;
    }}

    private static void CollectIncludePaths(
        Microsoft.EntityFrameworkCore.Metadata.IEntityType entityType,
        string prefix,
        HashSet<string> includePaths,
        HashSet<string> visitedTypes)
    {{
        var relations = entityType.GetNavigations(); 
        foreach (var relation in relations)
        {{
            var entityTypeName = relation.TargetEntityType.Name;
            if (visitedTypes.Contains(entityTypeName))
            {{
                continue;
            }}

            var navigationPath = string.IsNullOrEmpty(prefix) ? relation.Name : string.Concat(prefix, ""."", relation.Name);

            includePaths.Add(navigationPath);
            visitedTypes.Add(entityTypeName);
            
            CollectIncludePaths(relation.TargetEntityType, navigationPath, includePaths, visitedTypes);
            visitedTypes.Remove(entityTypeName);
        }}
    }}
    
    private static IQueryable<{item.ClassName}> ApplyIncludeRecursively(IQueryable<{item.ClassName}> query, string path, IncludeNode node)
    {{
        query = query.Include(path);

        foreach (var child in node.Children)
        {{
            var childPath = string.Concat(path, ""."", child.Key);
            query = ApplyIncludeRecursively(query, childPath, child.Value);
        }}

        return query;
    }}
}}";

            context.AddSource($"{item.ClassName}Controller.cs", generatedCode);
        }
    }

    private static string GenerateCollectionUpdateCode(SourceModel model)
    {
        var collectionProperties = model.PropertiesInfos.Where(ContainsCollectionTypeName).ToList();

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

            sb.AppendLine(
                $@"        // Update {prop.PropertyName} collection
        if (model.{prop.PropertyName} != null)
        {{
            var {propNameLower}Ids = model.{prop.PropertyName}.Select(m => m.Id).ToList();
            
            var existing{entityType.Pluralize()} = await dbContext.{entityType.Pluralize()}
                .Where(e => {propNameLower}Ids.Contains(e.Id))
                .ToListAsync();
            
            existingModel.{prop.PropertyName}.Clear();
            existingModel.{prop.PropertyName} = existing{entityType.Pluralize()};
        }}"
            );
        }

        return sb.ToString().TrimEnd();
    }

    private static string GenerateCollectionAddCode(SourceModel model)
    {
        var collectionProperties = model.PropertiesInfos.Where(ContainsCollectionTypeName).ToList();

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

            sb.AppendLine(
                $@"        var {propNameLower}Ids = model.{prop.PropertyName}?.Select(m => m.Id).ToList();
        
        if ({propNameLower}Ids != null && {propNameLower}Ids.Count > 0)
        {{
            var existing{entityType.Pluralize()} = await dbContext.{entityType.Pluralize()}
                .Where(e => {propNameLower}Ids.Contains(e.Id))
                .ToListAsync();

            if (existing{entityType.Pluralize()}.Any())
            {{
                modelToSave.{prop.PropertyName} = existing{entityType.Pluralize()};
            }}
        }}"
            );
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
        ImmutableArray<SourceModel> collectedData
    )
    {
        return collectedData
            .SelectMany(s =>
                s.PropertiesInfos.Where(p => p.PropertyType.Name.Contains("MediaInfo"))
                    .Select(p => new { s.ClassName, p.PropertyName })
            )
            .GroupBy(item => item.ClassName)
            .ToDictionary(g => g.Key, g => g.Select(item => item.PropertyName));
    }

    private static string GenerateMediaInfoCreationCode(
        string updatedModelName,
        string existingModelName,
        IEnumerable<string> mediaInfoPropertyNames
    )
    {
        if (!mediaInfoPropertyNames.Any())
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        foreach (var prop in mediaInfoPropertyNames)
        {
            sb.AppendLine(
                $@"
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
        }}"
            );
        }

        return sb.ToString().TrimEnd();
    }

    private static (string include, string remove) GenerateDeleteCodeForMediaInfo(SourceModel model)
    {
        var includeCode = new StringBuilder(
            $"var model = dbContext.{model.ClassName.Pluralize()} \n"
        );
        var removeCode = new StringBuilder();
        removeCode.AppendLine("");
        var mediaInfos = model
            .PropertiesInfos.Where(p => p.PropertyType.Name.Contains("MediaInfo"))
            .ToList();
        if (mediaInfos.Any())
        {
            foreach (var mediaInfo in mediaInfos)
            {
                removeCode.AppendLine(
                    $$"""         if(model.{{mediaInfo.PropertyName}} is not null){ """
                );
                includeCode.AppendLine(
                    $@"                  .Include(p => p.{mediaInfo.PropertyName})"
                );
                removeCode.AppendLine(
                    $@"
            try {{
                dbContext.Set<MediaInfo>().Attach(model.{mediaInfo.PropertyName}); 
                dbContext.Set<MediaInfo>().Remove(model.{mediaInfo.PropertyName});
                uploadService.DeleteMedia(model.{mediaInfo.PropertyName});
            }}
            catch(ArgumentNullException ex){{
                return NotFound($""Media URL not found for {mediaInfo.PropertyName}"");
            }}  
            catch(IOException ex){{
                return StatusCode(500, $""Internal server error: {{ex.Message}}"");
            }}
            catch(Exception ex){{
                return StatusCode(500, $""An unexpected error occurred: {{ex.Message}}"");            
            }}
            "
                );
                removeCode.AppendLine("         }");
                removeCode.AppendLine("");
            }
        }

        includeCode.AppendLine("                  .FirstOrDefault(p => p.Id == id);");
        return (includeCode.ToString().TrimEnd(), removeCode.ToString().TrimEnd());
    }

    private static string AggregateActions(
        SourceModel item,
        string includesCode,
        string collectionAddCode,
        string collectionUpdateCode,
        string mediaInfoUpdateCode,
        string includeCode,
        string removeCode
    )
    {
        var actions = new List<string>()
        {
            GenerateGetByIdAction(item.CrudActions, item, includesCode),
            GenerateGetAction(item.CrudActions, item, includesCode),
            GenerateGetAllAction(item.CrudActions, item, includesCode),
            GeneratePostAction(item.CrudActions, item, collectionAddCode),
            GeneratePostActionForMediaInfo(item.CrudActions, item),
            GeneratePutAction(
                item.CrudActions,
                item,
                includesCode,
                collectionUpdateCode,
                mediaInfoUpdateCode
            ),
            GeneratePatchAction(item.CrudActions, item, includesCode),
            GenerateDeleteAction(item.CrudActions, item, includeCode, removeCode),
        };
        var sb = new StringBuilder();
        foreach (var action in actions.Where(action => !string.IsNullOrEmpty(action)))
        {
            sb.Append(action);
        }
        return sb.ToString();
    }
}
