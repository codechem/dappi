using Microsoft.CodeAnalysis;

namespace CCApi.SourceGenerator.Extensions;

public static class NamespaceSymbolExtensions
{
    private static readonly List<string> NamespacesAnnotatingAfterRoot = ["Models", "Services", "Controllers"];
    public static string GetRootNamespace(this INamespaceSymbol namespaceSymbol)
    {
        if (namespaceSymbol is null || namespaceSymbol.IsGlobalNamespace)
        {
            return string.Empty;
        }

        var result = string.Empty;
        var current = namespaceSymbol;

        while (current.ContainingNamespace != null)
        {
            if (NamespacesAnnotatingAfterRoot.Any(p => p.Equals(current.Name, StringComparison.OrdinalIgnoreCase)))
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
}