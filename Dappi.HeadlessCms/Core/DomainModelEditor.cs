using System.Reflection;
using System.Text;
using Dappi.HeadlessCms.Core.Extensions;
using Dappi.HeadlessCms.Core.Schema;
using Dappi.HeadlessCms.Core.SyntaxVisitors;
using Dappi.HeadlessCms.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace Dappi.HeadlessCms.Core;

public class DomainModelEditor(string domainModelFolderPath)
{
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

    public string GenerateClassCode(Type modelType, bool isAuditableEntity)
    {
        var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        var assemblyName = assembly.GetName().Name;

        var classDeclaration = SyntaxFactory.ClassDeclaration(modelType.Name)
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
            .AddAttributeLists(RoslynHelpers.WithCcControllerAttribute())
            .AddMembers(RoslynHelpers.IdentityProperty()
                .WithAccessorList(RoslynHelpers.WithGetAndSet())
            );

        if (isAuditableEntity)
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

        return compilationUnit.NormalizeWhitespace().ToFullString();
    }

    

    public void GenerateProperty(
        string fieldName,
        string fieldType,
        string entityType,
        bool isRequired = false)
    {
        var filePath = Path.Combine(domainModelFolderPath, $"{entityType}.cs");
        var syntaxTree = RoslynHelpers.GetSyntaxTreeFromSource(filePath);
        var root = syntaxTree.GetCompilationUnitRoot();
        var classNode = root.DescendantNodes().FindClassDeclarationByName(entityType);
        
        if (classNode is null)
        {
            throw new Exception("Class not found");
        }
        
        var newProperty = RoslynHelpers.GenerateDynamicProperty(fieldType, fieldName, isRequired)
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
            .WithAccessorList(RoslynHelpers.WithGetAndSet());
        
        var newNode = classNode.AddMembers(newProperty);
        var newRoot = root.ReplaceNode(classNode, newNode);
        var newCode = newRoot.NormalizeWhitespace().ToFullString();
        File.WriteAllText(Path.Combine(domainModelFolderPath, $"{entityType}.cs"), newCode);
    }

    public void AddDomainModelEntityInfo(DomainModelEntityInfo entityInfo)
    {
        
    }

    private readonly UsingDirectiveSyntax[] _domainModelsUsingDirectives =
    [
        SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.ComponentModel.DataAnnotations")),
        SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.ComponentModel.DataAnnotations.Schema")),
        SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Dappi.SourceGenerator.Attributes")),
        SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Dappi.HeadlessCms.Models"))
    ];
}