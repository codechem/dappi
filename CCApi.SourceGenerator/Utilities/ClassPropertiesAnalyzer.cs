using System.Diagnostics;
using System.Text;
using CCApi.SourceGenerator.Models;
using Microsoft.CodeAnalysis;

namespace CCApi.SourceGenerator.Utilities;

public static class ClassPropertiesAnalyzer
{
    public static string GetIncludesIfAny(List<PropertyInfo> propertiesInfos)
    {
        var propertiesWithForeignKey = propertiesInfos.Where(p => !string.IsNullOrEmpty(p.PropertyForeignKey));
        if (!propertiesWithForeignKey.Any()) return string.Empty;

        var responseBuilder = new StringBuilder();
        foreach (var propertyInfo in propertiesWithForeignKey)
        {
            responseBuilder.Append(@$".Include(p => p.{propertyInfo.PropertyForeignKey})");
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
}