using CCApi.SourceGenerator.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CCApi.SourceGenerator.Extensions;

internal static class CompilationExtensions
{
    public static DbContextInformation? GetDbContextInformation(this Compilation compilation)
    {
        var dbContextSymbol = compilation.GetTypeByMetadataName("Microsoft.EntityFrameworkCore.DbContext");
        if (dbContextSymbol is null)
            return null; 

        foreach (var syntaxTree in compilation.SyntaxTrees)
        {
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var root = syntaxTree.GetRoot();

            var classDeclarations = root.DescendantNodes().OfType<ClassDeclarationSyntax>();

            foreach (var classDecl in classDeclarations)
            {
                if (semanticModel.GetDeclaredSymbol(classDecl) is not INamedTypeSymbol classSymbol)
                    continue;

                if (IsDerivedFrom(classSymbol, dbContextSymbol))
                {
                    return new DbContextInformation
                    {
                        ClassName = classSymbol.Name,
                        ResidingNamespace =  GetFullNamespace(classSymbol)
                    };
                }
            }
        }

        return null;
    }

    private static bool IsDerivedFrom(INamedTypeSymbol? symbol, INamedTypeSymbol baseType)
    {
        while (symbol != null)
        {
            if (SymbolEqualityComparer.Default.Equals(symbol.BaseType, baseType))
                return true;

            symbol = symbol.BaseType;
        }

        return false;
    }

    private static string GetFullNamespace(INamedTypeSymbol symbol)
    {
        var namespaces = new Stack<string>();
        var ns = symbol.ContainingNamespace;

        while (ns != null && !ns.IsGlobalNamespace)
        {
            namespaces.Push(ns.Name);
            ns = ns.ContainingNamespace;
        }

        return string.Join(".", namespaces);
    }
}