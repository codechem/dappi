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

                var authorizeAttributes = classSymbol.GetAttributes()
                    .Where(attr => attr.AttributeClass?.ToDisplayString() == "CCApi.SourceGenerator.Attributes.DappiAuthorizeAttribute")
                    .Select(attr =>
                    {
                        List<string> roles = new();
                        List<string> methods = new();

                        if (attr.ConstructorArguments.Length >= 2)
                        {
                            var roleArg = attr.ConstructorArguments[0];
                            if (roleArg.Kind == TypedConstantKind.Array)
                            {
                                roles = roleArg.Values
                                    .Select(v => v.Value?.ToString() ?? string.Empty)
                                    .ToList();
                            }

                            var methodArg = attr.ConstructorArguments[1];
                            if (methodArg.Kind == TypedConstantKind.Array)
                            {
                                methods = methodArg.Values
                                    .Select(v => v.Value?.ToString()?.ToUpperInvariant() ?? string.Empty)
                                    .ToList();
                            }
                        }

                        return new DappiAuthorizeInfo
                        {
                            Roles = roles,
                            Methods = methods
                        };
                    })
                    .ToList();

                return new SourceModel
                {
                    ClassName = classDeclaration.Identifier.Text,
                    ModelNamespace = classSymbol.ContainingNamespace.ToString() ?? string.Empty,
                    RootNamespace = classSymbol.ContainingNamespace.GetRootNamespace(),
                    PropertiesInfos = GoThroughPropertiesAndGatherInfo(namedClassTypeSymbol),
                    AuthorizeAttributes = authorizeAttributes
                };
            });

        var compilation = context.CompilationProvider.Combine(syntaxProvider.Collect());
        context.RegisterSourceOutput(compilation, Execute);
    }

    protected abstract void Execute(SourceProductionContext context,
        (Compilation Compilation, ImmutableArray<SourceModel> CollectedData) input);
}