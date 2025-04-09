using System.Collections.Immutable;
using CCApi.SourceGenerator.Extensions;
using CCApi.SourceGenerator.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static CCApi.SourceGenerator.Utilities.ClassPropertiesAnalyzer;

namespace CCApi.SourceGenerator.Generators;

public abstract class BaseSourceModelToSourceOutputGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var syntaxProvider = context.SyntaxProvider.ForAttributeWithMetadataName(
            "CCApi.SourceGenerator.Attributes.CCControllerAttribute",
            predicate: (node, _) => node is ClassDeclarationSyntax,
            transform: (ctx, _) => 
            {
                var classDeclaration = (ClassDeclarationSyntax)ctx.TargetNode;
                var classSymbol = ctx.TargetSymbol;
                var namedClassTypeSymbol = (INamedTypeSymbol)ctx.TargetSymbol;

                return new SourceModel
                {
                    ClassName = classDeclaration.Identifier.Text,
                    ModelNamespace = classSymbol.ContainingNamespace.ToString() ?? string.Empty,
                    RootNamespace = classSymbol.ContainingNamespace.GetRootNamespace(),
                    PropertiesInfos = GoThroughPropertiesAndGatherInfo(namedClassTypeSymbol)
                };
            });
        
        var compilation = context.CompilationProvider.Combine(syntaxProvider.Collect());
        context.RegisterSourceOutput(compilation, Execute);
    }

    protected abstract void Execute(SourceProductionContext context,
        (Compilation Compilation, ImmutableArray<SourceModel> CollectedData) input);
}