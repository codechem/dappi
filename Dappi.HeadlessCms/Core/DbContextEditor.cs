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

        if (existing is null)
        {
            return;
        }
        var assemblyReferences = AppDomain.CurrentDomain
            .GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location))
            .ToList();
        
        var newRoot = root.RemoveNode(existing, SyntaxRemoveOptions.KeepNoTrivia);
        var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
        
        var compilation = CSharpCompilation.Create("InMemoryAssembly")
            .AddSyntaxTrees(syntaxTree)
            .AddReferences(assemblyReferences)
            .WithOptions(options);
        
        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var validUsings = root.Usings
            .Where(u =>
            {
                var info = semanticModel.GetSymbolInfo(u.Name!);
                return info.Symbol != null;
            })
            .ToList();
        newRoot = newRoot.WithUsings(SyntaxFactory.List(validUsings));

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

    public void Cleanup()
    {
        var syntaxTree = GetSyntaxTreeFromDbContextSource();
        var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
        var assemblyReferences = AppDomain.CurrentDomain
            .GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location))
            .ToList();

        var comp = CSharpCompilation.Create("InMemoryAssembly")
            .AddSyntaxTrees(syntaxTree)
            .AddReferences(assemblyReferences)
            .WithOptions(options);

        using var ms = new MemoryStream();
        var emitResult = comp.Emit(ms);
        if (!emitResult.Success)
        {
            var diagnostics = emitResult.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToList();
            foreach (var diagnostic in diagnostics)
            {
                
            }
        }
    }

    public async Task SaveAsync()
    {
        if (HasChanges)
        {
            await File.WriteAllTextAsync(Path.Combine(dbContextFilePath, $"{dbContextName}.cs"), _currentCode);
            HasChanges = false;
        }
    }

    public void UpdateOnModelCreating(string modelName, string relatedTo, string relationshipType,
        string propertyName,
        string? relatedPropertyName = null)
    {
        var syntaxTree = GetSyntaxTreeFromDbContextSource();
        var root = syntaxTree.GetCompilationUnitRoot();
        var classNode = FindDbContextClassDeclaration(root);

        var onModelCreating = classNode.Members.OfType<MethodDeclarationSyntax>()
            .FirstOrDefault(m => m.Identifier.Text == OnModelCreatingMethodName);

        if (onModelCreating is null)
        {
            onModelCreating = SyntaxFactory
                .MethodDeclaration(
                    SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword))
                        .WithLeadingTrivia(SyntaxFactory.Space).WithTrailingTrivia(SyntaxFactory.Space),
                    OnModelCreatingMethodName)
                .AddModifiers(
                    SyntaxFactory.Token(SyntaxKind.ProtectedKeyword).WithLeadingTrivia(SyntaxFactory.Space)
                        .WithTrailingTrivia(SyntaxFactory.Space),
                    SyntaxFactory.Token(SyntaxKind.OverrideKeyword).WithLeadingTrivia(SyntaxFactory.Space)
                        .WithTrailingTrivia(SyntaxFactory.Space)
                )
                .WithParameterList(
                    SyntaxFactory.ParameterList(SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Parameter(SyntaxFactory.Identifier("modelBuilder"))
                            .WithType(SyntaxFactory.IdentifierName("ModelBuilder")
                                .WithTrailingTrivia(SyntaxFactory.Space))
                    ))
                )
                .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);
        }

        var relationCode = relationshipType switch
        {
            Constants.Relations.OneToOne => $@"modelBuilder.Entity<{modelName}>()
            .HasOne<{relatedTo}>(s => s.{propertyName})
            .WithOne(e => e.{relatedPropertyName ?? modelName})
            .HasForeignKey<{relatedTo}>(ad => ad.{modelName}Id);",

            Constants.Relations.OneToMany => $@"modelBuilder.Entity<{modelName}>()
            .HasMany<{relatedTo}>(s => s.{propertyName})
            .WithOne(e => e.{relatedPropertyName ?? modelName})
            .HasForeignKey(s => s.{relatedPropertyName ?? modelName}Id);",

            Constants.Relations.ManyToOne => $@"modelBuilder.Entity<{modelName}>()
            .HasOne<{relatedTo}>(s => s.{propertyName})
            .WithMany(e => e.{relatedPropertyName ?? $"{modelName.Pluralize()}"})
            .HasForeignKey(s => s.{propertyName}Id);",

            Constants.Relations.ManyToMany => $@"modelBuilder.Entity<{modelName}>()
            .HasMany(m => m.{propertyName})
            .WithMany(r => r.{relatedPropertyName})
            .UsingEntity(j => j.ToTable(""{modelName}{relatedTo.Pluralize()}""));",

            _ => throw new ArgumentException("Invalid relationship type")
        };

        var body = onModelCreating.Body ?? SyntaxFactory.Block();
        var newBody = body.AddStatements(SyntaxFactory.ParseStatement(relationCode));
        var newMethod = onModelCreating.WithBody(newBody);

        var baseOnModelCreating =
            newMethod?.Body?.Statements.FirstOrDefault(s => s.ToString().Contains(BaseOnModelCreating));
        if (baseOnModelCreating is not null)
        {
            newMethod = newMethod?.RemoveNode(baseOnModelCreating, SyntaxRemoveOptions.KeepNoTrivia);
        }

        newMethod = newMethod?.AddBodyStatements(SyntaxFactory.ParseStatement(BaseOnModelCreating));
        var newClassNode = classNode.RemoveNode(onModelCreating, SyntaxRemoveOptions.KeepNoTrivia);
        if (newMethod != null)
        {
            newClassNode = newClassNode?.AddMembers(newMethod);
        }

        if (newClassNode != null)
        {
            var newRoot = root.ReplaceNode(classNode, newClassNode);

            _currentCode = newRoot.NormalizeWhitespace().ToFullString();
        }

        HasChanges = true;
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