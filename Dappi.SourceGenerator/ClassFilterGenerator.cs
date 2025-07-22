using System.Collections.Immutable;
using Dappi.SourceGenerator.Generators;
using Dappi.SourceGenerator.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Dappi.SourceGenerator;

[Generator]
public class ClassFilterGenerator : BaseSourceModelToSourceOutputGenerator
{
    protected override void Execute(SourceProductionContext context,
        (Compilation Compilation, ImmutableArray<SourceModel> CollectedData) input)
    {
        var (_, collectedData) = input;
        
        foreach (var item in collectedData)
        {
            context.AddSource($"{item.ClassName}Filter.cs", @$"
namespace {item.RootNamespace}.Filtering;

public class {item.ClassName}Filter : PagingFilter
{{ 
    {string.Join("\n\t", item.PropertiesInfos.Where(p =>
        IsPrimitiveType(p.PropertyType.ToString())).Select(p => $"public {p.PropertyType}? {p.PropertyName} {{ get; set; }}"))}
}}");
        }
    }

    private static bool IsPrimitiveType(string type)
    {
        // TODO: add more types
        return type switch
        {
            "int" => true,
            "string" => true,
            "System.Guid" => true,
            "bool" => true,
            "double" => true,
            "float" => true,
            _ => false
        };
    }
}