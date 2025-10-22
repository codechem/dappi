using Dappi.HeadlessCms.Core.Extensions;
using Dappi.HeadlessCms.Core.Schema;
using Dappi.Core.Utils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Dappi.HeadlessCms.Core;

public class DbContextEditor(
    string dbContextFilePath,
    string dbContextName)
{
    private string _currentCode = string.Empty;
    private bool HasChanges { get; set; }

    public void AddDbSetToDbContext(DomainModelEntityInfo modelType)
    {
        HasChanges = false;
        var syntaxTree = GetSyntaxTreeFromDbContextSource();
        var root = syntaxTree.GetCompilationUnitRoot();
        var classNode = FindDbContextClassDeclaration(root);

        var modelName = modelType.Name;
        var propertyName = modelName.Pluralize();

        // Check if DbSet property already exists
        var existing = classNode.Members
            .OfType<PropertyDeclarationSyntax>()
            .Any(p =>
                p.Identifier.Text == propertyName &&
                p.Type is GenericNameSyntax { Identifier.Text: "DbSet" } generic &&
                generic.TypeArgumentList.Arguments.FirstOrDefault()?.ToString() == modelName);

        if (existing)
            return;

        var dbSetProperty = CreatePublicProperty(modelName, propertyName);

        var newClassNode = classNode.AddMembers(dbSetProperty);
        var newRoot = root.ReplaceNode(classNode, newClassNode);

        var apiNamespace = modelType.Namespace;
        if (newRoot.Usings.All(u => u.Name?.ToString() != apiNamespace))
        {
            var usingDirective = SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(apiNamespace))
                .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);

            newRoot = newRoot.AddUsings(usingDirective);
        }

        _currentCode = newRoot.NormalizeWhitespace().ToFullString();
        HasChanges = true;
    }
    
    public void RemoveSetFromDbContext(DomainModelEntityInfo modelType)
    {
        HasChanges = false;
        var syntaxTree = GetSyntaxTreeFromDbContextSource();
        var root = syntaxTree.GetCompilationUnitRoot();
        var classNode = FindDbContextClassDeclaration(root);

        var modelName = modelType.Name;
        var propertyName = $"{modelName}s";

        var existing = classNode.Members
            .OfType<PropertyDeclarationSyntax>()
            .FirstOrDefault(p =>
                p.Identifier.Text == propertyName &&
                p.Type is GenericNameSyntax { Identifier.Text: "DbSet" } generic &&
                generic.TypeArgumentList.Arguments.FirstOrDefault()?.ToString() == modelName);
      
        if (existing is not null)
        {
            var newRoot = root.RemoveNode(existing, SyntaxRemoveOptions.KeepNoTrivia);
            _currentCode = newRoot.NormalizeWhitespace().ToFullString()!;
            return;
        }

        HasChanges = true;
    }
    
    private static PropertyDeclarationSyntax CreatePublicProperty(string modelName, string propertyName)
    {
        return SyntaxFactory
            .PropertyDeclaration(
                SyntaxFactory.GenericName("DbSet")
                    .WithTypeArgumentList(
                        SyntaxFactory.TypeArgumentList(
                            SyntaxFactory.SingletonSeparatedList<TypeSyntax>(
                                SyntaxFactory.IdentifierName(modelName)))),
                SyntaxFactory.Identifier(propertyName))
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
            .WithAccessorList(
                SyntaxFactory.AccessorList(SyntaxFactory.List([
                    SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                        .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                    SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                        .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                ])))
            .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);
    }
    
    public async Task SaveAsync()
    {
        if (HasChanges)
            await File.WriteAllTextAsync(Path.Combine(dbContextFilePath, $"{dbContextName}.cs"), _currentCode);
    }
    
    private ClassDeclarationSyntax FindDbContextClassDeclaration(CompilationUnitSyntax root)
    {
        var classNode = root.DescendantNodes()
            .FindClassDeclarationByName(dbContextName!);

        if (classNode == null)
            throw new InvalidOperationException("DbContext class not found");
        
        return classNode;
    }
    
    private SyntaxTree GetSyntaxTreeFromDbContextSource()
    {
        var dbContextSourceCode = File.ReadAllText(Path.Combine(dbContextFilePath, $"{dbContextName}.cs"!));
        var syntaxTree = CSharpSyntaxTree.ParseText(dbContextSourceCode);
        return syntaxTree;
    }
}
