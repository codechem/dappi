using System.Reflection;
using Dappi.Core.Attributes;
using Dappi.Core.Enums;
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

        public static AttributeListSyntax WithCcControllerAttribute(List<CrudActions>? crudActionsList)
        {
            List<AttributeArgumentSyntax> arguments = [];
            var attribute = SyntaxFactory.Attribute(SyntaxFactory.ParseName(CcControllerAttribute.ShortName));
            if (crudActionsList is null || crudActionsList.Count <= 0)
            {
                return SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(attribute));
            }

            arguments.AddRange(crudActionsList.Select(a =>
                SyntaxFactory.AttributeArgument(SyntaxFactory.ParseExpression($"{nameof(CrudActions)}.{a}"))));
            attribute = attribute.AddArgumentListArguments(arguments.ToArray());

            return SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(attribute));

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

        private static PropertyDeclarationSyntax WithRegexAttribute(this PropertyDeclarationSyntax property, string attributeName, params AttributeArgumentSyntax[] arguments)
        {
            var attribute = SyntaxFactory.Attribute(SyntaxFactory.ParseName(attributeName));
            if (arguments.Length > 0)
            {
                attribute = attribute.AddArgumentListArguments(arguments);
            }
            var attributeList = SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(attribute));
            return property.AddAttributeLists(attributeList);
        }

        public static PropertyDeclarationSyntax WithRegularExpressionAttribute(this PropertyDeclarationSyntax property,
            string regex)
        {
            return property.WithRegexAttribute("RegularExpression", SyntaxFactory.AttributeArgument(SyntaxFactory.ParseExpression($"@\"{regex}\"")));
        }

        public static PropertyDeclarationSyntax WithFutureDateAttribute(this PropertyDeclarationSyntax property)
        {
            var attribute = SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("FutureDate"));
            var attributeList = SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(attribute));
            return property.AddAttributeLists(attributeList);
        }
        public static PropertyDeclarationSyntax WithLengthAttribute(this PropertyDeclarationSyntax property,
            string? minLength, string? maxLength)
        {
            const int DefaultMaxLength = 1000;
            
            var min = string.IsNullOrEmpty(minLength) ? "0" : minLength;
            var max = string.IsNullOrEmpty(maxLength) ? DefaultMaxLength.ToString() : maxLength;
            
            var attribute = SyntaxFactory.Attribute(SyntaxFactory.ParseName("Length"));
            var arguments = new[]
            {
                SyntaxFactory.AttributeArgument(SyntaxFactory.ParseExpression(min)),
                SyntaxFactory.AttributeArgument(SyntaxFactory.ParseExpression(max))
            };
            
            attribute = attribute.WithArgumentList(
                SyntaxFactory.AttributeArgumentList(SyntaxFactory.SeparatedList(arguments))
            );
            
            var attributeList = SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(attribute));
            return property.AddAttributeLists(attributeList);
        }

        public static PropertyDeclarationSyntax WithRangeAttribute(this PropertyDeclarationSyntax property,
            string? minValue, string? maxValue, string propertyType)
        {
            var attribute = SyntaxFactory.Attribute(SyntaxFactory.ParseName("Range"));
            var arguments = new List<AttributeArgumentSyntax>();
            
            if (string.IsNullOrEmpty(minValue) && string.IsNullOrEmpty(maxValue))
            {
                return property;
            }
            
            double? minDouble = null;
            double? maxDouble = null;
            
            if (!string.IsNullOrEmpty(minValue))
            {
                if (double.TryParse(minValue, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var parsedMin))
                {
                    minDouble = parsedMin;
                }
            }
            
            if (!string.IsNullOrEmpty(maxValue))
            {
                if (double.TryParse(maxValue, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var parsedMax))
                {
                    maxDouble = parsedMax;
                }
            }
            
            if (minDouble == null)
            {
                minDouble = propertyType.ToLower() switch
                {
                    "int" => int.MinValue,
                    "float" => float.MinValue,
                    "double" => double.MinValue,
                    "decimal" => (double)decimal.MinValue,
                    "long" => long.MinValue,
                    "short" => short.MinValue,
                    "byte" => byte.MinValue,
                    _ => double.MinValue
                };
            }
            
            if (maxDouble == null)
            {
                maxDouble = propertyType.ToLower() switch
                {
                    "int" => int.MaxValue,
                    "float" => float.MaxValue,
                    "double" => double.MaxValue,
                    "decimal" => (double)decimal.MaxValue,
                    "long" => long.MaxValue,
                    "short" => short.MaxValue,
                    "byte" => byte.MaxValue,
                    _ => double.MaxValue
                };
            }
            
            var minLiteral = SyntaxFactory.LiteralExpression(
                SyntaxKind.NumericLiteralExpression,
                SyntaxFactory.Literal(minDouble.Value.ToString(System.Globalization.CultureInfo.InvariantCulture), minDouble.Value));
            var maxLiteral = SyntaxFactory.LiteralExpression(
                SyntaxKind.NumericLiteralExpression,
                SyntaxFactory.Literal(maxDouble.Value.ToString(System.Globalization.CultureInfo.InvariantCulture), maxDouble.Value));
            
            arguments.Add(SyntaxFactory.AttributeArgument(minLiteral));
            arguments.Add(SyntaxFactory.AttributeArgument(maxLiteral));
            
            attribute = attribute.WithArgumentList(
                SyntaxFactory.AttributeArgumentList(SyntaxFactory.SeparatedList(arguments))
            );
            
            var attributeList = SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(attribute));
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