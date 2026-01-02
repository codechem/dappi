using System.Reflection;
using Dappi.Core.Attributes;
using Dappi.Core.Enums;
using Dappi.HeadlessCms.Core.Attributes;
using Dappi.HeadlessCms.Core.Extensions;
using Dappi.HeadlessCms.Core.Schema;
using Dappi.HeadlessCms.Core.SyntaxVisitors;
using Dappi.HeadlessCms.Exceptions;
using Dappi.HeadlessCms.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Dappi.HeadlessCms.Core;

public class DomainModelEditor(string domainModelFolderPath , string enumsFolderPath)
{
    private bool HasChanges { get; set; }
    private readonly Dictionary<string, string> _codeChanges = new();
    
    public async Task<DomainModelEntityInfo[]> GetDomainModelEntityInfosAsync()
    {
        var modelFiles = Directory.GetFiles(domainModelFolderPath, "*.cs", SearchOption.AllDirectories);

        var syntaxTreeMap = new Dictionary<string, SyntaxTree>();

        foreach (var file in modelFiles)
        {
            var code = await File.ReadAllTextAsync(file);
            var tree = CSharpSyntaxTree.ParseText(code, path: file);
            syntaxTreeMap[file] = tree;
        }

        var tasks = modelFiles.Select(async file =>
        {
            var syntaxTree = syntaxTreeMap[file];
            var root = await syntaxTree.GetRootAsync().ConfigureAwait(false);
            var visitor = new DomainModelToSchemaVisitor();
            visitor.Visit(root);
            return visitor.Result;
        });

        var results = await Task.WhenAll(tasks);
        return results.Where(r => r != null).ToArray()!;
    }

    public void CreateEntityModel(ModelRequest request)
    {
        var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        var assemblyName = assembly.GetName().Name;

        var classDeclaration = SyntaxFactory.ClassDeclaration(request.ModelName)
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
            .AddAttributeLists(RoslynHelpers.WithCcControllerAttribute(request.CrudActions))
            .AddMembers(RoslynHelpers.IdentityProperty()
                .WithAccessorList(RoslynHelpers.WithGetAndSet())
            );

        if (request.IsAuditableEntity)
        {
            classDeclaration = classDeclaration.AddBaseListTypes(
                SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName(nameof(IAuditableEntity)))
            ).AddMembers(RoslynHelpers.GeneratePropertiesFromType(typeof(IAuditableEntity)));
        }

        var namesSpaceDeclaration = SyntaxFactory
            .NamespaceDeclaration(SyntaxFactory.ParseName(assemblyName + ".Entities"))
            .AddMembers(classDeclaration);

        var compilationUnit = SyntaxFactory.CompilationUnit()
            .AddUsings(_domainModelsUsingDirectives)
            .AddMembers(namesSpaceDeclaration);

        var code = compilationUnit.NormalizeWhitespace().ToFullString();
        _codeChanges[request.ModelName] = code;
        HasChanges = true;
    }

    public void DeleteRelatedProperties(string modelName, string relatedModelName)
    {
        var filePath = Path.Combine(domainModelFolderPath, $"{modelName}.cs");

        var syntaxTree = _codeChanges.TryGetValue(modelName, out var value)
            ? CSharpSyntaxTree.ParseText(value)
            : RoslynHelpers.GetSyntaxTreeFromSource(filePath);

        var root = syntaxTree.GetCompilationUnitRoot();
        var classNode = root.DescendantNodes().FindClassDeclarationByName(modelName);

        if (classNode == null)
        {
            throw new ModelNotFoundException($"Class {modelName} not found.", modelName);
        }
        
        var properties = GetRelatedPropertiesForDeletion
            (classNode, relatedModelName, DappiRelationAttribute.ShortName);
        var newRoot = root.RemoveNodes(properties, SyntaxRemoveOptions.KeepNoTrivia);
        if (newRoot == null)
        {
            throw new InvalidOperationException("Failed to remove properties.");
        }

        var newCode = newRoot.NormalizeWhitespace().ToFullString();
        if (!string.IsNullOrEmpty(newCode))
        {
            _codeChanges[modelName] = newCode;
        }

        HasChanges = true;
    }

    public void ConfigureActions(string modelName, CrudActions[] actions)
    {
        var filePath = Path.Combine(domainModelFolderPath, $"{modelName}.cs");
        var syntaxTree = _codeChanges.TryGetValue(modelName, out var value)
            ? CSharpSyntaxTree.ParseText(value)
            : RoslynHelpers.GetSyntaxTreeFromSource(filePath);

        var root = syntaxTree.GetCompilationUnitRoot();
        var classNode = root.DescendantNodes().FindClassDeclarationByName(modelName);
        if (classNode == null)
        {
            throw new ModelNotFoundException($"Class {modelName} not found.", modelName);
        }
        
        List<AttributeArgumentSyntax> arguments = [];
        arguments.AddRange(actions.Select(a =>
            SyntaxFactory.AttributeArgument(SyntaxFactory.ParseExpression($"{nameof(CrudActions)}.{a}"))));
        
        var attribute = classNode.AttributeLists.First(x =>
                x.Attributes.Any(a => a.Name.ToString() == CcControllerAttribute.ShortName))
            .Attributes.First();
        var newAttribute = attribute.WithArgumentList(SyntaxFactory.AttributeArgumentList(SyntaxFactory.SeparatedList(arguments)));
        var newClassNode = classNode.ReplaceNode(attribute, newAttribute);
        var newRoot = root.ReplaceNode(classNode, newClassNode);
        var newCode = newRoot.NormalizeWhitespace().ToFullString();
        _codeChanges[modelName] = newCode;
        HasChanges = true;
    }

    public void AddProperty(Property property)
    {
        var filePath = Path.Combine(domainModelFolderPath, $"{property.DomainModel}.cs");
        var syntaxTree = _codeChanges.TryGetValue(property.DomainModel, out var value)
            ? CSharpSyntaxTree.ParseText(value)
            : RoslynHelpers.GetSyntaxTreeFromSource(filePath);

        var root = syntaxTree.GetCompilationUnitRoot();
        var classNode = root.DescendantNodes().FindClassDeclarationByName(property.DomainModel);

        if (classNode is null)
        {
            throw new ModelNotFoundException($"Class {property.DomainModel} not found.", property.DomainModel);
        }

        var newProperty = RoslynHelpers.GenerateDynamicProperty(property.Type, property.Name, property.IsRequired)
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
            .WithAccessorList(RoslynHelpers.WithGetAndSet());
        if (property.RelationKind is not null)
        {
            newProperty = newProperty.WithRelationAttribute(property.RelationKind, property.RelatedDomainModel);
        }
        if (property.Type == "string" && !string.IsNullOrEmpty(property.Regex))
        {
            newProperty = newProperty.WithRegularExpressionAttribute(property.Regex);
        }
        if (property.NoPastDates && IsDateType(property.Type))
        {
            newProperty = newProperty.WithFutureDateAttribute();
        }

        var newNode = classNode.AddMembers(newProperty);
        var newRoot = root.ReplaceNode(classNode, newNode);
        var newCode = newRoot.NormalizeWhitespace().ToFullString();

        _codeChanges[property.DomainModel] = newCode;
        HasChanges = true;
    }

    public Property? GetProperty(string modelName, string propertyName)
    {
        var filePath = Path.Combine(domainModelFolderPath, $"{modelName}.cs");
        var syntaxTree = _codeChanges.TryGetValue(modelName, out var value)
            ? CSharpSyntaxTree.ParseText(value)
            : RoslynHelpers.GetSyntaxTreeFromSource(filePath);

        var root = syntaxTree.GetCompilationUnitRoot();
        var classNode = root.DescendantNodes().FindClassDeclarationByName(modelName);

        if (classNode is null) return null;
        
        var propertyNode = classNode.DescendantNodes()
            .OfType<PropertyDeclarationSyntax>()
            .FirstOrDefault(p => p.Identifier.Text == propertyName);

        if (propertyNode is null) return null;

        var propertyType = propertyNode.Type.ToString().Replace("?", "");
        var isRequired = propertyNode.Modifiers.Any(m => m.IsKind(SyntaxKind.RequiredKeyword)) ||
                        !propertyNode.Type.ToString().Contains("?");

        DappiRelationKind? relationKind = null;
        string? relatedModel = null;
        string? regex = null;

        var relationAttribute = propertyNode.AttributeLists
            .SelectMany(al => al.Attributes)
            .FirstOrDefault(a => a.Name.ToString() == DappiRelationAttribute.ShortName);

        if (relationAttribute?.ArgumentList?.Arguments.Count >= 2)
        {
            var relationKindArg = relationAttribute.ArgumentList.Arguments[0].Expression.ToString();
            if (Enum.TryParse<DappiRelationKind>(relationKindArg.Split('.').Last(), out var parsedKind))
            {
                relationKind = parsedKind;
            }
            relatedModel = relationAttribute.ArgumentList.Arguments[1].Expression.ToString().Trim('"');
        }

        var regexAttribute = propertyNode.AttributeLists
            .SelectMany(al => al.Attributes)
            .FirstOrDefault(a => a.Name.ToString() == "RegularExpression");

        if (regexAttribute?.ArgumentList?.Arguments.Count >= 1)
        {
            regex = regexAttribute.ArgumentList.Arguments[0].Expression.ToString().Trim('"');
        }

        var hasFutureDateAttribute = propertyNode.AttributeLists
            .SelectMany(al => al.Attributes)
            .Any(a => a.Name.ToString() == "FutureDate");

        return new Property
        {
            DomainModel = modelName,
            Name = propertyName,
            Type = propertyType,
            IsRequired = isRequired,
            RelationKind = relationKind,
            RelatedDomainModel = relatedModel,
            Regex = regex,
            NoPastDates = hasFutureDateAttribute,
            HasIndex = false
        };
    }

    public void UpdateProperty(string modelName, string oldPropertyName, Property newProperty)
    {
        var filePath = Path.Combine(domainModelFolderPath, $"{modelName}.cs");
        var syntaxTree = _codeChanges.TryGetValue(modelName, out var value)
            ? CSharpSyntaxTree.ParseText(value)
            : RoslynHelpers.GetSyntaxTreeFromSource(filePath);

        var root = syntaxTree.GetCompilationUnitRoot();
        var classNode = root.DescendantNodes().FindClassDeclarationByName(modelName);

        if (classNode is null)
        {
            throw new ModelNotFoundException($"Class {modelName} not found.", modelName);
        }

        var oldPropertyNode = classNode.DescendantNodes()
            .OfType<PropertyDeclarationSyntax>()
            .FirstOrDefault(p => p.Identifier.Text == oldPropertyName);

        if (oldPropertyNode is null)
        {
            throw new PropertyNotFoundException($"Property {oldPropertyName} not found in model {modelName}.", typeof(object), oldPropertyName);
        }

        var updatedProperty = RoslynHelpers.GenerateDynamicProperty(newProperty.Type, newProperty.Name, newProperty.IsRequired)
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
            .WithAccessorList(RoslynHelpers.WithGetAndSet());

        if (newProperty.RelationKind is not null)
        {
            updatedProperty = updatedProperty.WithRelationAttribute(newProperty.RelationKind, newProperty.RelatedDomainModel);
        }
        if (newProperty.Type == "string" && !string.IsNullOrEmpty(newProperty.Regex))
        {
            updatedProperty = updatedProperty.WithRegularExpressionAttribute(newProperty.Regex);
        }
        if (newProperty.NoPastDates && IsDateType(newProperty.Type))
        {
            updatedProperty = updatedProperty.WithFutureDateAttribute();
        }

        var newClassNode = classNode.ReplaceNode(oldPropertyNode, updatedProperty);
        var newRoot = root.ReplaceNode(classNode, newClassNode);
        var newCode = newRoot.NormalizeWhitespace().ToFullString();

        _codeChanges[modelName] = newCode;
        HasChanges = true;
    }

    private static bool IsDateType(string type)
    {
        var normalizedType = type.TrimEnd('?');
        return normalizedType is "DateTime" or "DateTimeOffset" or "DateOnly";
    }

    public async Task SaveAsync()
    {
        if (HasChanges)
        {
            var tasks = new List<Task>();
            foreach (var (file, newCode) in _codeChanges)
            {
                var path = Path.Combine(domainModelFolderPath, $"{file}.cs");
                tasks.Add(File.WriteAllTextAsync(path, newCode));
            }

            await Task.WhenAll(tasks);
            HasChanges = false;
        }
    }

    public List<PropertyDeclarationSyntax> GetPropertiesContainingAttribute(string modelName,
        string attributeName)
    {
        var filePath = Path.Combine(domainModelFolderPath, $"{modelName}.cs");
        var syntaxTree = RoslynHelpers.GetSyntaxTreeFromSource(filePath);
        var root = syntaxTree.GetCompilationUnitRoot();
        var classNode = root.DescendantNodes().FindClassDeclarationByName(modelName);
        if (classNode == null)
        {
            return [];
        }

        var properties = classNode.DescendantNodes().OfType<PropertyDeclarationSyntax>()
            .Where(p =>
                p.AttributeLists.Any(a => a.Attributes.Any(a1 => a1.Name.ToString() == attributeName)));
        return properties.ToList();
    }

    public List<PropertyDeclarationSyntax> GetRelatedPropertiesForDeletion
        (ClassDeclarationSyntax classNode, string relatedModelName, string attributeName)
    {
        var properties = classNode.DescendantNodes().OfType<PropertyDeclarationSyntax>()
            .Where(p =>
                p.AttributeLists.Any(a => a.Attributes.Any(a1 => a1.Name.ToString() == attributeName))
                && p.AttributeLists.Any(a =>
                    a.Attributes.Any(a1 =>
                        a1.ArgumentList != null &&
                        a1.ArgumentList.Arguments.Any(a2 => a2.ToString().Contains(relatedModelName)))));
        
        return properties.ToList();
    }
    
    public List<string> GetRelatedEntities(List<PropertyDeclarationSyntax> properties)
    {
        List<string> relatedModels = [];
        var propertyType = string.Empty;

        foreach (var property in properties)
        {
            if (property.Type is IdentifierNameSyntax)
            {
                var typeSyntax = (IdentifierNameSyntax)property.Type;
                propertyType = GetPropertyType(typeSyntax);
            }
            else if (property.Type is GenericNameSyntax)
            {
                var typeSyntax = (GenericNameSyntax)property.Type;
                propertyType = GetPropertyType(typeSyntax);
            }
            else if (property.Type is NullableTypeSyntax nullableTypeSyntax)
            {
                if (nullableTypeSyntax.ElementType is IdentifierNameSyntax)
                {
                    var identifierNameSyntax = (IdentifierNameSyntax)nullableTypeSyntax.ElementType;
                    propertyType = GetPropertyType(identifierNameSyntax);
                }

                if (nullableTypeSyntax.ElementType is GenericNameSyntax)
                {
                    var genericNameSyntax = (GenericNameSyntax)nullableTypeSyntax.ElementType;
                    propertyType = GetPropertyType(genericNameSyntax);
                }
            }

            if (!string.IsNullOrEmpty(propertyType) &&
                !relatedModels.Contains(propertyType) &&
                propertyType != nameof(Guid))
            {
                relatedModels.Add(propertyType);
            }
        }

        return relatedModels;
    }

    public void RemoveEnumProperty(string modelName , string enumName)
    {
        var filePath = Path.Combine(domainModelFolderPath, $"{modelName}.cs");
        var syntaxTree = _codeChanges.TryGetValue(modelName, out var value)
            ? CSharpSyntaxTree.ParseText(value)
            : RoslynHelpers.GetSyntaxTreeFromSource(filePath);
        var root = syntaxTree.GetCompilationUnitRoot();
        var classNode = root.DescendantNodes().FindClassDeclarationByName(modelName);

        if (classNode is null)
        {
            throw new ModelNotFoundException($"Class {modelName} not found.", modelName);
        }

        var properties = classNode.DescendantNodes().OfType<PropertyDeclarationSyntax>()
            .Where(p => (p.Type is IdentifierNameSyntax identifierNameSyntax && identifierNameSyntax.Identifier.Text == enumName) || 
                        p.Type is NullableTypeSyntax nullableType &&
                        nullableType.ElementType is IdentifierNameSyntax nameSyntax &&
                        nameSyntax.Identifier.Text == enumName)
            .ToList();
        var newRoot = root.RemoveNodes(properties, SyntaxRemoveOptions.KeepNoTrivia);
        
        if (newRoot == null) 
            return;
        
        var newCode = newRoot.NormalizeWhitespace().ToFullString();
        _codeChanges[modelName] = newCode;
        HasChanges = true;
    }
    
    public void UpdateUsings(DomainModelEntityInfo model)
    {
        var filePath = Path.Combine(domainModelFolderPath, $"{model.Name}.cs");
        var syntaxTree = _codeChanges.TryGetValue(model.Name, out var value)
            ? CSharpSyntaxTree.ParseText(value)
            : RoslynHelpers.GetSyntaxTreeFromSource(filePath);
        var root = syntaxTree.GetCompilationUnitRoot();
        var usings = root.Usings.Where(u => u.Name != null && !u.Name.ToString().Contains("Enums"));
        var newRoot = root.WithUsings(SyntaxFactory.List(usings));
        var newCode = newRoot.NormalizeWhitespace().ToFullString();
        _codeChanges[model.Name] = newCode;
        HasChanges = true;
    }
    public void AddEnumNamespaceIfMissing(string modelName)
    {
        var filePath = Path.Combine(domainModelFolderPath, $"{modelName}.cs");
        var syntaxTree = _codeChanges.TryGetValue(modelName, out var value)
            ? CSharpSyntaxTree.ParseText(value)
            : RoslynHelpers.GetSyntaxTreeFromSource(filePath);
        var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        var enumsAssemblyName = $"{assembly.GetName().Name}.Enums";
        var root = syntaxTree.GetCompilationUnitRoot();
        var newRoot = root.AddUsings(
            SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(enumsAssemblyName))
            );
        var newCode = newRoot.NormalizeWhitespace().ToFullString();
        _codeChanges[modelName] = newCode;
        HasChanges = true;
    }
    public string GenerateEnumCode(string enumName, Dictionary<string, int> enumValues)
    {
        var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        var assemblyName = assembly.GetName().Name;
    
        var enumDeclaration = SyntaxFactory.EnumDeclaration(enumName)
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));
        
        var sorted = enumValues.OrderBy(kv => kv.Value).ToList();
    
        foreach (var item in sorted)
        {
            var newMember = SyntaxFactory.EnumMemberDeclaration(item.Key).WithEqualsValue(
                SyntaxFactory.EqualsValueClause(
                    SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression,
                        SyntaxFactory.Literal(item.Value))));
            enumDeclaration = enumDeclaration.AddMembers(newMember);
        }

        var namespaceDeclaration = SyntaxFactory
            .NamespaceDeclaration(SyntaxFactory.ParseName(assemblyName + ".Enums"))
            .AddMembers(enumDeclaration);
    
        var compilationUnit = SyntaxFactory.CompilationUnit()
            .AddMembers(namespaceDeclaration);
    
        return compilationUnit.NormalizeWhitespace().ToFullString();
    }

    public async Task DeleteEnum(string enumName)
    {
        var filePath = Path.Combine(enumsFolderPath, $"{enumName}.cs");
        var models = await GetDomainModelEntityInfosAsync();
        File.Delete(filePath);
        foreach (var model in models)
        {
            RemoveEnumProperty(model.Name, enumName);
            if (!Directory.EnumerateFiles(enumsFolderPath, "*.cs", SearchOption.AllDirectories).Any())
            {
                UpdateUsings(model);
            }
        }
        await SaveAsync();
    }

    public void AddDomainModelEntityInfo(DomainModelEntityInfo entityInfo)
    {
        
    }
    
    private string? GetPropertyType(GenericNameSyntax genericNameSyntax)
    {
        return genericNameSyntax.TypeArgumentList.Arguments.FirstOrDefault()?.ToString();
    }

    private string GetPropertyType(IdentifierNameSyntax identifierNameSyntax)
    {
        return identifierNameSyntax.Identifier.Text;
    }
    
    private readonly UsingDirectiveSyntax[] _domainModelsUsingDirectives =
    [
        SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.ComponentModel.DataAnnotations")),
        SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.ComponentModel.DataAnnotations.Schema")),
        SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Dappi.HeadlessCms.Models")),
        SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Dappi.Core.Attributes")),
        SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Dappi.HeadlessCms.Core.Attributes")),
        SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Dappi.Core.Enums"))
        
    ];
}