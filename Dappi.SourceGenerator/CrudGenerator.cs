using System.Collections.Immutable;
using System.Text;
using Dappi.Core.Enums;
using Dappi.SourceGenerator.Extensions;
using Dappi.SourceGenerator.Generators;
using Dappi.SourceGenerator.Models;
using Dappi.Core.Utils;
using Microsoft.CodeAnalysis;
using static Dappi.SourceGenerator.Utilities.ClassPropertiesAnalyzer;
using static Dappi.SourceGenerator.Generators.ActionsGenerator;

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
            var hasAuthorizationOnControllerLevel = item.AuthorizeAttributes.FirstOrDefault() is
                { OnControllerLevel: true };
            var authorizeTag = hasAuthorizationOnControllerLevel ? "[Authorize]" : null;
            var mediaInfoUpdateCode = string.Empty;
            if (mediaInfoPropertyNames.ContainsKey(item.ClassName))
            {
                mediaInfoUpdateCode =
                    GenerateMediaInfoCreationCode("model", "existingModel", mediaInfoPropertyNames[item.ClassName]);
            }

            (string includeCode, string removeCode) = GenerateDeleteCodeForMediaInfo(item);
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

{authorizeTag}
[ApiController]
[Route(""api/[controller]"")]
public partial class {item.ClassName}Controller(
    {dbContextData.ClassName} dbContext, 
    IMediaUploadService uploadService) : ControllerBase
{{

    {AggregateActions(item,includesCode,collectionAddCode,collectionUpdateCode,mediaInfoUpdateCode,includeCode,removeCode)}    

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

    private static string AggregateActions(SourceModel item, string includesCode, string collectionAddCode , string collectionUpdateCode, string mediaInfoUpdateCode, string includeCode, string removeCode)
    {
        var actions = new List<string>()
        {
            GenerateGetByIdAction(item.CrudActions, item, includesCode),
            GenerateGetAction(item.CrudActions, item, includesCode),
            GenerateGetAllAction(item.CrudActions, item, includesCode),
            GeneratePostAction(item.CrudActions, item, collectionAddCode),
            GeneratePostActionForMediaInfo(item.CrudActions, item),
            GeneratePutAction(item.CrudActions, item, includesCode, collectionUpdateCode, mediaInfoUpdateCode),
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