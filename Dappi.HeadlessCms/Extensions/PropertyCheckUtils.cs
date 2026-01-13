using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Dappi.HeadlessCms.Extensions
{
    public static class PropertyCheckUtils
    {
        public static bool PropertyNameExists(ClassDeclarationSyntax classDeclaration, string fieldName)
        {
            var properties = classDeclaration.Members
                .OfType<PropertyDeclarationSyntax>()
                .Select(p => p.Identifier.Text);

            return properties.Any(p => p.Equals(fieldName, StringComparison.Ordinal));
        }
    }
}