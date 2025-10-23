using Dappi.HeadlessCms.Core.Attributes;
using Dappi.HeadlessCms.Core.Extensions;
using Dappi.HeadlessCms.Core.Schema;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Dappi.HeadlessCms.Core.SyntaxVisitors;

public class DomainModelToSchemaVisitor : CSharpSyntaxWalker
{
    public DomainModelEntityInfo? Result { get; private set; }
    private readonly List<EnumProperty> _enumValues = new();

    public override void VisitCompilationUnit(CompilationUnitSyntax node)
    {
        // Visit all enum declarations first
        foreach (var enumDecl in node.DescendantNodes().OfType<EnumDeclarationSyntax>())
        {
            VisitEnumDeclaration(enumDecl);
        }

        foreach (var classDecl in node.DescendantNodes().OfType<ClassDeclarationSyntax>())
        {
            VisitClassDeclaration(classDecl);
        }
    }
    
    public override void VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        var attributeShortName = nameof(CCControllerAttribute).Replace("Attribute", "");

        var hasCcAttribute = node.AttributeLists
            .SelectMany(al => al.Attributes)
            .Any(attr => attr.Name.ToString().Contains(attributeShortName));
      
        if (!hasCcAttribute)
            return;

        var props = new Dictionary<string, DappiPropertyInfo>();

        foreach (var p in node.Members.OfType<PropertyDeclarationSyntax>())
        {
            var name = p.Identifier.Text;
            var typeText = p.Type.ToString();
            var isRequired = !IsPropertyNullable(p);

            DappiPropertyInfo attribute;

            if (IsDropdown(p))
            {
                attribute = new EnumProperty()
                {
                    Type = "dropdown",
                    Required = isRequired,
                    Values = GetEnumValues(p)
                };
            }
            // else if (IsMedia(p))
            // {
            //     attribute = new MediaProperty()
            //     {
            //         Type = "media",
            //         Required = isRequired,
            //         AllowedTypes = 
            //     };
            // }
            else if (IsRelation(p, out var relType, out var target, out var mappedBy, out var inversedBy))
            {
                attribute = new RelationProperty()
                {
                    Type = "relation",
                    Required = isRequired,
                    RelationType = relType,
                    Target = target,
                    MappedBy = mappedBy,
                    InversedBy = inversedBy
                };
            }
            else
            {
                attribute = new StringProperty()
                {
                    Type = typeText,
                    Required = isRequired
                };
            }

            props[name] = attribute;
        }

        var ns = node.GetNamespaceDeclaration();
        var namespaceName = ns switch
        {
            NamespaceDeclarationSyntax nds => nds.Name.ToString(),
            FileScopedNamespaceDeclarationSyntax fnds => fnds.Name.ToString(),
            _ => string.Empty
        };

        Result = new DomainModelEntityInfo
        {
            Name = node.Identifier.Text, Namespace = namespaceName, Properties = props
        };
    }
    //
    // private bool IsMedia(PropertyDeclarationSyntax propertyDeclarationSyntax, out List<string> allowedMediaTypes)
    // {
    //     allowedMediaTypes = [];
    //     if (propertyDeclarationSyntax.Type.ToString() == nameof(MediaInfo))
    //     {
    //         allowedMediaTypes = prop
    //         return true;
    //     }
    //
    //     return false;
    // }

    public override void VisitEnumDeclaration(EnumDeclarationSyntax node)
    {
        var enumVals = new List<string>();
     
        foreach (var p in node.Members.OfType<EnumMemberDeclarationSyntax>())
        {
            enumVals.Add(p.Identifier.Text);
        }

        _enumValues.Add( new EnumProperty() { Type = node.Identifier.Text, Values = enumVals, Required = true});            
    }

    private List<string> GetEnumValues(PropertyDeclarationSyntax property)
    {
        var enumType = property.Type.ToString().TrimEnd('?');
        return _enumValues.FirstOrDefault(val => val.Type == enumType)?.Values ?? [];
    }

    private static bool IsDropdown(PropertyDeclarationSyntax propertyDeclarationSyntax)
    {
        const string attributeShortName = nameof(DappiDropdown);

        return propertyDeclarationSyntax.AttributeLists
            .SelectMany(al => al.Attributes)
            .Any(attr => attr.Name.ToString().Contains(attributeShortName));
    }

    private static bool IsPropertyNullable(PropertyDeclarationSyntax propertyDeclarationSyntax)
    {
        var typeSyntax = propertyDeclarationSyntax.Type;

        if (typeSyntax is NullableTypeSyntax)
            return true;

        var typeText = typeSyntax.ToString();
        return typeText.EndsWith('?');
    }
    
    private static bool IsRelation(
        PropertyDeclarationSyntax property,
        out DappiRelationKind relationType,
        out string target,
        out string? mappedBy,
        out string? inversedBy)
    {
        relationType = 0;
        target = null!;
        mappedBy = null;
        inversedBy = null;

        var attributeShortName = nameof(DappiRelationAttribute).Replace("Attribute", "");

        var relationAttribute = property.AttributeLists
            .SelectMany(al => al.Attributes)
            .FirstOrDefault(attr => attr.Name.ToString().Contains(attributeShortName));

        if (relationAttribute is null)
            return false;

        // 1. Get the relation type from the first argument (e.g. "manyToOne")
        if (relationAttribute.ArgumentList?.Arguments.Count > 0)
        {
            var cleanName =  relationAttribute.ArgumentList.Arguments[0].ToString().Trim('"').Replace($"{nameof(DappiRelationKind)}.","");

            Enum.TryParse(cleanName, out DappiRelationKind relationKind);
            relationType = relationKind;
        }
        else
        {
            return false;
        }

        target = ExtractTargetTypeName(property.Type.ToString());

        if (relationType == DappiRelationKind.ManyToOne || relationType == DappiRelationKind.OneToOne)
        {
            mappedBy = property.Identifier.Text;
        }
        else if (relationType ==  DappiRelationKind.OneToMany || relationType == DappiRelationKind.ManyToMany)
        {
            inversedBy = property.Identifier.Text;
        }

        return true;
    }
    private static string ExtractTargetTypeName(string typeText)
    {
        if (typeText.EndsWith("?"))
            typeText = typeText.TrimEnd('?');

        var genericStart = typeText.IndexOf('<');
        var genericEnd = typeText.IndexOf('>');

        if (genericStart != -1 && genericEnd != -1 && genericEnd > genericStart)
        {
            return typeText.Substring(genericStart + 1, genericEnd - genericStart - 1).Trim();
        }

        return typeText;
    }
}