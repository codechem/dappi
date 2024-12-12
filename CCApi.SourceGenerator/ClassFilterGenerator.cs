using System.Collections.Immutable;
using CCApi.SourceGenerator.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static CCApi.SourceGenerator.Utilities.ClassPropertiesAnalyzer;

namespace CCApi.SourceGenerator;

[Generator]
public class ClassFilterGenerator : IIncrementalGenerator
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
            context.AddSource($"{item.ClassName}Filter.cs", @$"
namespace CC.ApiGen.Filtering;

public class {item.ClassName}Filter : PagingFilter
{{ 
    {string.Join("\n\t", item.PropertiesInfos.Where(p => IsPrimitiveType(p.PropertyType.ToString())).Select(p => $"public {p.PropertyType} {p.PropertyName} {{ get; set; }}"))}
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