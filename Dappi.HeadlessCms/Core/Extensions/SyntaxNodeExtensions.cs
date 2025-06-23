using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Dappi.HeadlessCms.Core.Extensions
{
    public static class SyntaxNodeExtensions
    {
        public static ClassDeclarationSyntax? FindClassDeclarationByName(this IEnumerable<SyntaxNode> nodes, string className)
        {
            return nodes.OfType<ClassDeclarationSyntax>()
                .FirstOrDefault(cds => cds.Identifier.Text == className);
        }
        
        public static SyntaxNode? GetNamespaceDeclaration(this SyntaxNode node)
        {
           return node.Ancestors()
                .FirstOrDefault(a =>
                    a is NamespaceDeclarationSyntax or FileScopedNamespaceDeclarationSyntax);
        }
    }
}