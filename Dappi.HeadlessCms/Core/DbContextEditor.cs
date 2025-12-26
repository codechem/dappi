using System.Reflection;
using Dappi.HeadlessCms.Core.Extensions;
using Dappi.HeadlessCms.Core.Schema;
using Dappi.Core.Utils;
using Dappi.HeadlessCms.Extensions;
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
    private const string BaseOnModelCreating = "base.OnModelCreating(modelBuilder);";
    private const string OnModelCreatingMethodName = "OnModelCreating";

    public void AddDbSetToDbContext(DomainModelEntityInfo modelType)
    {
        var syntaxTree = GetSyntaxTreeFromDbContextSource();
        var root = syntaxTree.GetCompilationUnitRoot();
        var classNode = FindDbContextClassDeclaration(root);

        var modelName = modelType.Name;
        var propertyName = modelName.Pluralize();

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
        var syntaxTree = string.IsNullOrWhiteSpace(_currentCode)
            ? GetSyntaxTreeFromDbContextSource()
            : CSharpSyntaxTree.ParseText(_currentCode);
        var root = syntaxTree.GetCompilationUnitRoot();
        var classNode = FindDbContextClassDeclaration(root);

        var modelName = modelType.Name;
        var propertyName = modelName.Pluralize();
        var existing = classNode.Members
            .OfType<PropertyDeclarationSyntax>()
            .FirstOrDefault(p =>
                p.Identifier.Text == propertyName &&
                p.Type is GenericNameSyntax { Identifier.Text: "DbSet" } generic &&
                generic.TypeArgumentList.Arguments.FirstOrDefault()?.ToString() == modelName);

        if (existing is null)
        {
            return;
        }

        var newRoot = root.RemoveNode(existing, SyntaxRemoveOptions.KeepNoTrivia);

        _currentCode = newRoot?.NormalizeWhitespace().ToFullString()!;

        HasChanges = true;
    }

    public void UpdateUsings()
    {
        var syntaxTree = string.IsNullOrWhiteSpace(_currentCode)
            ? GetSyntaxTreeFromDbContextSource()
            : CSharpSyntaxTree.ParseText(_currentCode);
        var root = syntaxTree.GetCompilationUnitRoot();
        var classNode = FindDbContextClassDeclaration(root);
        var hasDbSet = classNode.Members
            .OfType<PropertyDeclarationSyntax>()
            .Any(p =>
                p.Type is GenericNameSyntax { Identifier.Text: "DbSet" });

        if (hasDbSet)
        {
            return;
        }

        var usings = root.Usings.Where(u => u.Name != null && !u.Name.ToString().Contains("Entities"));
        var newRoot = root?.WithUsings(SyntaxFactory.List(usings));
        _currentCode = newRoot?.NormalizeWhitespace().ToFullString()!;
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
            .WithAccessorList(RoslynHelpers.WithGetAndSet())
            .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);
    }

    public async Task SaveAsync()
    {
        if (HasChanges)
        {
            await File.WriteAllTextAsync(Path.Combine(dbContextFilePath, $"{dbContextName}.cs"), _currentCode);
            HasChanges = false;
        }
    }

    public void UpdateOnModelCreatingWithIndexedColumn(string modelName, string propertyName)
    {
        var indexStatement =
            $@"modelBuilder.Entity<{modelName}>().HasIndex(e => e.{propertyName});";

        UpdateOnModelCreatingPrivate(indexStatement);
    }

    public void UpdateOnModelCreating(
        string modelName,
        string relatedTo,
        string relationshipType,
        string propertyName,
        string? relatedPropertyName = null)
    {
        var relationCode = relationshipType switch
        {
            Constants.Relations.OneToOne => $@"modelBuilder.Entity<{modelName}>()
            .HasOne<{relatedTo}>(s => s.{propertyName})
            .WithOne(e => e.{relatedPropertyName ?? modelName})
            .HasForeignKey<{relatedTo}>(ad => ad.{modelName}Id);",

            Constants.Relations.OneToMany => $@"modelBuilder.Entity<{modelName}>()
            .HasMany<{relatedTo}>(s => s.{propertyName})
            .WithOne(e => e.{relatedPropertyName ?? modelName})
            .HasForeignKey(s => s.{modelName}Id);",

            Constants.Relations.ManyToOne => $@"modelBuilder.Entity<{modelName}>()
            .HasOne<{relatedTo}>(s => s.{propertyName})
            .WithMany(e => e.{relatedPropertyName ?? $"{modelName.Pluralize()}"})
            .HasForeignKey(s => s.{relatedTo}Id);",

            Constants.Relations.ManyToMany => $@"modelBuilder.Entity<{modelName}>()
            .HasMany(m => m.{propertyName})
            .WithMany(r => r.{relatedPropertyName})
            .UsingEntity(j => j.ToTable(""{modelName}{relatedTo.Pluralize()}""));",

            _ => throw new ArgumentException("Invalid relationship type")
        };

        UpdateOnModelCreatingPrivate(relationCode);
    }

    private void UpdateOnModelCreatingPrivate(string statementCode)
    {
        var syntaxTree = GetSyntaxTreeFromDbContextSource();
        var root = syntaxTree.GetCompilationUnitRoot();
        var classNode = FindDbContextClassDeclaration(root);

        var onModelCreating = GetOrCreateOnModelCreatingMethod(classNode);

        var body = onModelCreating.Body ?? SyntaxFactory.Block();
        body = body.AddStatements(SyntaxFactory.ParseStatement(statementCode));

        var baseCall = body.Statements
            .FirstOrDefault(s => s.ToString().Contains(BaseOnModelCreating));

        if (baseCall is not null)
        {
            body = body.RemoveNode(baseCall, SyntaxRemoveOptions.KeepNoTrivia);
        }

        body = body.AddStatements(SyntaxFactory.ParseStatement(BaseOnModelCreating));

        var updatedMethod = onModelCreating.WithBody(body);

        var updatedClass = classNode
            .RemoveNode(onModelCreating, SyntaxRemoveOptions.KeepNoTrivia)
            ?.AddMembers(updatedMethod);

        if (updatedClass is not null)
        {
            var newRoot = root.ReplaceNode(classNode, updatedClass);
            _currentCode = newRoot.NormalizeWhitespace().ToFullString();
        }

        HasChanges = true;
    }
    
    public void DeleteRelations(string entity, List<string> relatedEntities)
    {
        var syntaxTree = string.IsNullOrWhiteSpace(_currentCode)
            ? GetSyntaxTreeFromDbContextSource()
            : CSharpSyntaxTree.ParseText(_currentCode);
        var root = syntaxTree.GetCompilationUnitRoot();
        var classNode = FindDbContextClassDeclaration(root);

        var onModelCreating = classNode.Members.OfType<MethodDeclarationSyntax>()
            .FirstOrDefault(m => m.Identifier.Text == OnModelCreatingMethodName);

        if (onModelCreating is null)
        {
            return;
        }

        var block = onModelCreating.Body;
        var statementsToRemove = new List<StatementSyntax>();

        foreach (var relation in relatedEntities)
        {
            var relationStatements = block?.Statements
                .OfType<ExpressionStatementSyntax>()
                .Where(s => s.ToString().Contains(relation) && s.ToString().Contains(entity))
                .ToList();

            if (relationStatements != null)
            {
                statementsToRemove.AddRange(relationStatements);
            }
        }

        var modifiedStatements = block.Statements.Except(statementsToRemove).ToList();
        var modifiedBlock = block.WithStatements(SyntaxFactory.List(modifiedStatements));

        var modifiedMethod = onModelCreating.WithBody(modifiedBlock);
        var modifiedRoot = root.ReplaceNode(onModelCreating, modifiedMethod);
        _currentCode = modifiedRoot.NormalizeWhitespace().ToFullString();
        HasChanges = true;
    }

    private MethodDeclarationSyntax GetOrCreateOnModelCreatingMethod(ClassDeclarationSyntax classNode)
    {
        var existingMethod = classNode.Members
            .OfType<MethodDeclarationSyntax>()
            .FirstOrDefault(m => m.Identifier.Text == OnModelCreatingMethodName);

        if (existingMethod is not null)
        {
            return existingMethod;
        }

        return SyntaxFactory.MethodDeclaration(
                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
                OnModelCreatingMethodName)
            .AddModifiers(
                SyntaxFactory.Token(SyntaxKind.ProtectedKeyword),
                SyntaxFactory.Token(SyntaxKind.OverrideKeyword))
            .WithParameterList(
                SyntaxFactory.ParameterList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Parameter(SyntaxFactory.Identifier("modelBuilder"))
                            .WithType(SyntaxFactory.IdentifierName("ModelBuilder")))))
            .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);
    }
    
    private ClassDeclarationSyntax FindDbContextClassDeclaration(CompilationUnitSyntax root)
    {
        var classNode = root.DescendantNodes()
            .FindClassDeclarationByName(dbContextName);

        if (classNode == null)
            throw new InvalidOperationException("DbContext class not found");

        return classNode;
    }

    private SyntaxTree GetSyntaxTreeFromDbContextSource()
    {
        var dbContextSourceCode = File.ReadAllText(Path.Combine(dbContextFilePath, $"{dbContextName}.cs"));
        var syntaxTree = CSharpSyntaxTree.ParseText(dbContextSourceCode);
        return syntaxTree;
    }
}