using System.Collections.Immutable;
using Dappi.Core.Attributes;
using Dappi.Core.Extensions;
using Dappi.SourceGenerator.Extensions;
using Dappi.SourceGenerator.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Dappi.SourceGenerator.Utilities.ClassPropertiesAnalyzer;

namespace Dappi.SourceGenerator.Generators;

public abstract class BaseSourceModelToSourceOutputGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var syntaxProvider = context.SyntaxProvider.ForAttributeWithMetadataName(
            typeof(CcControllerAttribute).FullName ?? throw new NullReferenceException(),
            predicate: (node, _) => node is ClassDeclarationSyntax,
            transform: (ctx, _) =>
            {
                var classDeclaration = (ClassDeclarationSyntax)ctx.TargetNode;
                var classSymbol = ctx.TargetSymbol;
                var namedClassTypeSymbol = (INamedTypeSymbol)ctx.TargetSymbol;
                var authorizeAttributeName =
                    typeof(DappiAuthorizeAttribute).FullName ?? throw new NullReferenceException();

                var crudActions = classDeclaration.ExtractAllowedCrudActions();

                var authorizeAttributes = classSymbol.GetAttributes()
                    .Where(attr => attr.AttributeClass?.ToDisplayString() == authorizeAttributeName)
                    .Select(attr =>
                    {
                        List<string> roles = [];
                        List<string> methods = [];

                        if (attr.NamedArguments.Length <= 0)
                        {
                            return new DappiAuthorizeInfo { Roles = roles, Methods = methods };
                        }

                        foreach (var namedArg in attr.NamedArguments)
                        {
                            if (namedArg is { Key: nameof(DappiAuthorizeAttribute.Roles), Value.Kind: TypedConstantKind.Array })
                            {
                                roles = namedArg.Value.Values
                                    .Select(v => v.Value?.ToString() ?? string.Empty)
                                    .ToList();
                            }

                            if (namedArg is { Key: nameof(DappiAuthorizeAttribute.Methods), Value.Kind: TypedConstantKind.Array })
                            {
                                methods = namedArg.Value.Values
                                    .Select(v =>
                                    {
                                        if (v is { Value: not null, Type: INamedTypeSymbol { TypeKind: TypeKind.Enum } enumType })
                                        {
                                            var enumMember = enumType.GetMembers()
                                                .OfType<IFieldSymbol>()
                                                .FirstOrDefault(f => f.IsConst && Equals(f.ConstantValue, v.Value));

                                            return enumMember?.Name.ToUpperInvariant() ?? string.Empty;
                                        }
                                        return v.Value?.ToString()?.ToUpperInvariant() ?? string.Empty;
                                    })
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
                    AuthorizeAttributes = authorizeAttributes,
                    CrudActions = crudActions.ToList()
                };
            });

        var compilation = context.CompilationProvider.Combine(syntaxProvider.Collect());
        context.RegisterSourceOutput(compilation, Execute);
    }

    protected abstract void Execute(SourceProductionContext context,
        (Compilation Compilation, ImmutableArray<SourceModel> CollectedData) input);
}