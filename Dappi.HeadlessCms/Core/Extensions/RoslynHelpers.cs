using System.Reflection;
using Dappi.Core.Attributes;
using Dappi.HeadlessCms.Core.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Dappi.HeadlessCms.Core.Extensions
{
    public static class RoslynHelpers
    {
        public static AccessorListSyntax WithGetAndSet(SyntaxKind getKind = SyntaxKind.GetAccessorDeclaration,
            SyntaxKind setKind = SyntaxKind.SetAccessorDeclaration)
        {
            return SyntaxFactory.AccessorList(SyntaxFactory.List([
                SyntaxFactory.AccessorDeclaration(getKind)
                    .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                SyntaxFactory.AccessorDeclaration(setKind)
                    .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
            ]));
        }

        public static AttributeListSyntax WithCcControllerAttribute()
        {
            return SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(
                SyntaxFactory.Attribute(
                    SyntaxFactory.ParseName(nameof(CCControllerAttribute).Replace("Attribute", "")))));
        }

        public static PropertyDeclarationSyntax IdentityProperty()
        {
            return SyntaxFactory.PropertyDeclaration(SyntaxFactory.IdentifierName("Guid"), "Id")
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .AddAttributeLists(
                    SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Attribute(SyntaxFactory.ParseName("Key"))
                    )),
                    SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Attribute(SyntaxFactory.ParseName("DatabaseGenerated"))
                            .AddArgumentListArguments(SyntaxFactory.AttributeArgument(
                                SyntaxFactory.ParseExpression("DatabaseGeneratedOption.Identity")))))
                );
        }

        public static PropertyDeclarationSyntax WithRelationAttribute(this PropertyDeclarationSyntax property,
            DappiRelationKind? relationType = null, string? relatedType = null)
        {
            var attributeList = SyntaxFactory.AttributeList(
                SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.Attribute(
                            SyntaxFactory.ParseName(DappiRelationAttribute.ShortName))
                        .AddArgumentListArguments(
                            SyntaxFactory.AttributeArgument(
                                SyntaxFactory.ParseExpression($"{nameof(DappiRelationKind)}.{relationType}")),
                            SyntaxFactory.AttributeArgument(SyntaxFactory.ParseExpression($"typeof({relatedType})"))
                        )
                )
            );

            return property.AddAttributeLists(attributeList);
        }

        public static MemberDeclarationSyntax[] GeneratePropertiesFromType(Type type)
        {
            var memberList = new List<MemberDeclarationSyntax>();

            var typeMembers = type.GetProperties(BindingFlags.Public |
                                                 BindingFlags.Instance |
                                                 BindingFlags.DeclaredOnly);

            foreach (var member in typeMembers)
            {
                var newMember = GenerateDynamicProperty(member.PropertyType.GetDisplayName(), member.Name,
                        !member.IsNullable())
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                    .WithAccessorList(WithGetAndSet());
                memberList.Add(newMember);
            }

            return memberList.ToArray();
        }

        public static PropertyDeclarationSyntax GenerateDynamicProperty(string propertyType, string propertyName,
            bool isRequired = false)
        {
            if (isRequired)
            {
                return SyntaxFactory.PropertyDeclaration(SyntaxFactory.IdentifierName(propertyType), propertyName);
            }

            return SyntaxFactory
                .PropertyDeclaration(
                    SyntaxFactory.NullableType(SyntaxFactory.IdentifierName(propertyType),
                        SyntaxFactory.Token(SyntaxKind.QuestionToken)), propertyName);
        }

        public static SyntaxTree GetSyntaxTreeFromSource(string filePath)
        {
            var dbContextSourceCode = File.ReadAllText(filePath);
            var syntaxTree = CSharpSyntaxTree.ParseText(dbContextSourceCode);
            return syntaxTree;
        }
    }
}