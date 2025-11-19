using System.Text;
using Dappi.Core.Attributes;
using Dappi.SourceGenerator.Models;
using Microsoft.CodeAnalysis;

namespace Dappi.SourceGenerator.Utilities;

public static class ClassPropertiesAnalyzer
{
    public static string GetIncludesIfAny(List<PropertyInfo> propertiesInfos, Dictionary<string, IEnumerable<string>> mediaInfoPropertyNames, string sourceModelClassName)
    {
        var propertiesWithForeignKey = propertiesInfos
            .Where(p => !string.IsNullOrEmpty(p.PropertyForeignKey))
            .ToList();

        var collectionProperties = propertiesInfos
            .Where(ContainsCollectionTypeName)
            .ToList();
        
        if (!mediaInfoPropertyNames.ContainsKey(sourceModelClassName) && !propertiesWithForeignKey.Any() && !collectionProperties.Any())
            return string.Empty;

        var responseBuilder = new StringBuilder();

        if (mediaInfoPropertyNames.TryGetValue(sourceModelClassName, out var propertyNames))
        {
            foreach (var propertyName in propertyNames)
            {
                responseBuilder.AppendLine();
                responseBuilder.Append(@$"            .Include(p => p.{propertyName})");
            }
        }
        
        foreach (var propertyInfo in propertiesWithForeignKey)
        {
            if (propertyInfo.GenericTypeName == "Guid")
            {
                responseBuilder.Append(GenerateIncludesCode(mediaInfoPropertyNames, propertyInfo.PropertyForeignKey, propertyInfo.PropertyForeignKey));
                
                continue;
            }

            responseBuilder.Append(GenerateIncludesCode(mediaInfoPropertyNames, propertyInfo.GenericTypeName, propertyInfo.PropertyName));
        }

        foreach (var propertyInfo in collectionProperties)
        {
            responseBuilder.Append(GenerateIncludesCode(mediaInfoPropertyNames, propertyInfo.GenericTypeName, propertyInfo.PropertyName));
        }

        return responseBuilder.ToString();
    }

    public static string PrintPropertyInfos(List<PropertyInfo> propertiesInfos)
    {
        var builder = new StringBuilder();
        foreach (var property in propertiesInfos)
        {
            builder.Append($"Property: {property.PropertyName}, Type: {property.PropertyType}, ");
            builder.Append("Attributes:");
            foreach (var attribute in property.PropertyAttributes)
            {
                builder.Append($"  - {attribute}");
            }

            builder.Append($"\nForeignKeyAttribute: {property.PropertyForeignKey}");

            builder.Append("\n==================================\n");
        }

        return builder.ToString();
    }

    public static string PrintDappiAuthorizeInfos(List<DappiAuthorizeInfo> dappiAuthorizeInfos)
    {
        var builder = new StringBuilder();

        foreach (var info in dappiAuthorizeInfos)
        {
            builder.Append("Methods:");
            builder.Append(string.Join(", ", info.Methods));
            builder.Append("Roles:");
            builder.Append(string.Join(", ", info.Roles));
        }

        return builder.ToString();
    }

    public static string PropagateDappiAuthorizationTags(List<DappiAuthorizeInfo> dappiAuthorizeInfos,
        AuthorizeMethods httpMethod)
    {
        foreach (var dappiInfo in dappiAuthorizeInfos)
        {
            if (dappiInfo.Methods.Contains(httpMethod.ToString().ToUpperInvariant()))
            {
                if (dappiInfo.Roles.Any())
                {
                    var rolesString = string.Join(",", dappiInfo.Roles.Select(r => $"\"{r}\""));
                    return $"[Authorize(Roles = {rolesString})]";
                }
                if (dappiInfo.IsAuthenticated)
                {
                    return "[Authorize]";
                }
            }
        }

        return string.Empty;
    }

    private static string GetSimpleTypeName(ITypeSymbol typeSymbol)
    {
        if (typeSymbol is INamedTypeSymbol namedTypeSymbol)
        {
            return namedTypeSymbol.Name;
        }

        return typeSymbol.ToDisplayString();
    }

    private static string GetFormattedTypeName(ITypeSymbol typeSymbol)
    {
        if (typeSymbol is INamedTypeSymbol namedTypeSymbol)
        {
            string baseName = namedTypeSymbol.Name;

            if (namedTypeSymbol.IsGenericType && namedTypeSymbol.TypeArguments.Length > 0)
            {
                string genericArguments = string.Join(", ", namedTypeSymbol.TypeArguments
                    .Select(t => GetSimpleTypeName(t)));
                return $"{genericArguments}";
            }

            return baseName;
        }

        return typeSymbol.ToDisplayString();
    }


    public static List<PropertyInfo> GoThroughPropertiesAndGatherInfo(INamedTypeSymbol classSymbol)
    {
        var propertiesInfo = classSymbol.GetMembers()
            .OfType<IPropertySymbol>()
            .Select(property =>
            {
                var propertyName = property.Name;
                var propertyType = property.Type;
                var genericTypeName = GetFormattedTypeName(propertyType);
                var propertyForeignKey = string.Empty;
                var propertyAttributes = property.GetAttributes()
                    // .Select(attr => attr.AttributeClass?.ToDisplayString())
                    .Select(attr =>
                    {
                        // Check if the attribute is ForeignKeyAttribute
                        if (attr.AttributeClass?.ToDisplayString() == "System.ComponentModel.DataAnnotations.Schema.ForeignKeyAttribute")
                        {
                            // Get the constructor argument value (e.g., "Author")
                            var foreignKeyName = attr.ConstructorArguments.FirstOrDefault().Value?.ToString();
                            propertyForeignKey = foreignKeyName;
                            return $"{attr.AttributeClass?.Name} (ForeignKey Name: {foreignKeyName})";
                        }

                        return attr.AttributeClass?.ToDisplayString();
                    })
                    .Where(attrName => attrName != null)
                    .ToList();

                return new PropertyInfo
                {
                    PropertyName = propertyName,
                    PropertyType = propertyType,
                    PropertyAttributes = propertyAttributes,
                    PropertyForeignKey = propertyForeignKey,
                    GenericTypeName = genericTypeName
                };
            }).ToList();
        return propertiesInfo;
    }

    private static bool ContainsCollectionTypeName(PropertyInfo propertyInfo)
    {
        return propertyInfo.PropertyType.Name.Contains("IEnumerable")
            || propertyInfo.PropertyType.Name.Contains("List")
            || propertyInfo.PropertyType.Name.Contains("ICollection");
    }

    private static string GenerateIncludesCode(Dictionary<string, IEnumerable<string>> mediaInfoPropertyNames, string propertyTypeName, string propertyName)
    {
        StringBuilder stringBuilder = new StringBuilder();
        
        if (mediaInfoPropertyNames.TryGetValue(propertyTypeName, out var mediaInfoNames))
        {
            foreach (var mediaInfoName in mediaInfoNames)
            {
                stringBuilder.AppendLine();
                stringBuilder.Append(@$"            .Include(p => p.{propertyName})");
                stringBuilder.AppendLine();
                stringBuilder.Append(@$"                .ThenInclude(p => p.{mediaInfoName})");
            }

            return stringBuilder.ToString();
        }

        stringBuilder.AppendLine();
        stringBuilder.Append(@$"            .Include(p => p.{propertyName})");

        return stringBuilder.ToString();
    }
}