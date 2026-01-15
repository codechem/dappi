using Dappi.Core.Attributes;
using Dappi.Core.Enums;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Dappi.Core.Extensions
{
    public static class ClassDeclarationSyntaxExtensions
    {
        public static IEnumerable<CrudActions> ExtractAllowedCrudActions(this ClassDeclarationSyntax classDeclaration)
        {
            var ccAttr = classDeclaration.AttributeLists
                .SelectMany(attrList => attrList.Attributes)
                .FirstOrDefault(x => x.Name.ToString() == CcControllerAttribute.ShortName);

            var arguments = ccAttr?.ArgumentList?.Arguments.ToList();
            
            if (arguments != null && arguments.Count > 0)
            {
                return arguments.Select(x => Enum.TryParse(x.ToString().Split('.').Last(), true, out CrudActions action)
                    ? action
                    : throw new ArgumentException("Invalid action argument"));
            }
            return CcControllerAttribute.DefaultActions.ToList();
        }

        public static bool PropertyNameExists(this ClassDeclarationSyntax classDeclaration, string fieldName)
        {
            var properties = classDeclaration.Members
                .OfType<PropertyDeclarationSyntax>()
                .Select(p => p.Identifier.Text);

            return properties.Any(p => p.Equals(fieldName, StringComparison.Ordinal));
        }
    }
}