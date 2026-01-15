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
        var root = GetRoot();
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
        var root = GetRoot();
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

    private MethodDeclarationSyntax? GetOnModelCreatingMethod(CompilationUnitSyntax root)
    {
        var classNode = FindDbContextClassDeclaration(root);

        var onModelCreating = classNode.Members
            .OfType<MethodDeclarationSyntax>()
            .FirstOrDefault(m => m.Identifier.Text == OnModelCreatingMethodName);

        return onModelCreating;
    }

    public void DeleteRelations(string entity, List<string> relatedEntities)
    {
        var root = GetRoot();
        var onModelCreating = GetOnModelCreatingMethod(root);

        if (onModelCreating is null)
            return;

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

    private CompilationUnitSyntax GetRoot()
    {
        var syntaxTree = string.IsNullOrWhiteSpace(_currentCode)
            ? GetSyntaxTreeFromDbContextSource()
            : CSharpSyntaxTree.ParseText(_currentCode);
        return syntaxTree.GetCompilationUnitRoot();
    }

    public void UpdatePropertyNameInOnModelCreating(string modelName, string oldPropertyName, string newPropertyName)
    {
        var root = GetRoot();
        var onModelCreating = GetOnModelCreatingMethod(root);
        
        if (onModelCreating?.Body == null) return;

        var nodesToReplace = new Dictionary<SyntaxNode, SyntaxNode>();

        var invocations = onModelCreating
            .DescendantNodes()
            .OfType<InvocationExpressionSyntax>();

        foreach (var invocation in invocations)
        {
            if (invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
                memberAccess.Name is GenericNameSyntax genericName &&
                genericName.Identifier.Text == "Entity")
            {
                var typeArg = genericName.TypeArgumentList.Arguments.FirstOrDefault();
                if (typeArg?.ToString() != modelName)
                    continue;

                var propertyAccesses = invocation
                    .Ancestors()
                    .TakeWhile(n => n is not ExpressionStatementSyntax)
                    .SelectMany(n => n.DescendantNodes().OfType<MemberAccessExpressionSyntax>())
                    .Where(ma => ma.Name.Identifier.Text == oldPropertyName);

                foreach (var propertyAccess in propertyAccesses)
                {
                    var newPropertyAccess = propertyAccess.WithName(
                        SyntaxFactory.IdentifierName(newPropertyName));

                    nodesToReplace[propertyAccess] = newPropertyAccess;
                }
            }
        }

        if (!nodesToReplace.Any())
            return;
        
        var modifiedRoot = root.ReplaceNodes(
            nodesToReplace.Keys,
            (oldNode, _) => nodesToReplace[oldNode]);

        _currentCode = modifiedRoot.NormalizeWhitespace().ToFullString();
        HasChanges = true;
    }


    public void RemovePropertyFromOnModelCreating(string modelName, string propertyName)
    {
        var root = GetRoot();
        var classNode = FindDbContextClassDeclaration(root);

        var onModelCreating = classNode.Members.OfType<MethodDeclarationSyntax>()
            .FirstOrDefault(m => m.Identifier.Text == OnModelCreatingMethodName);

        if (onModelCreating?.Body == null) return;

        var block = onModelCreating.Body;
        var statementsToRemove = new List<StatementSyntax>();

        foreach (var statement in block.Statements.OfType<ExpressionStatementSyntax>())
        {
            if (StatementReferencesProperty(statement, modelName, propertyName))
            {
                statementsToRemove.Add(statement);
            }
        }

        if (statementsToRemove.Any())
        {
            var modifiedStatements = block.Statements.Except(statementsToRemove).ToList();
            var modifiedBlock = block.WithStatements(SyntaxFactory.List(modifiedStatements));
            var modifiedMethod = onModelCreating.WithBody(modifiedBlock);
            var modifiedRoot = root.ReplaceNode(onModelCreating, modifiedMethod);
            _currentCode = modifiedRoot.NormalizeWhitespace().ToFullString();
            HasChanges = true;
        }
    }

    private bool StatementReferencesProperty(ExpressionStatementSyntax statement, string modelName, string propertyName)
    {
        var invocations = statement.DescendantNodes().OfType<InvocationExpressionSyntax>();
        
        bool hasEntityOfModel = false;
        bool referencesProperty = false;

        foreach (var invocation in invocations)
        {
            if (invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
                memberAccess.Name is GenericNameSyntax genericName &&
                genericName.Identifier.Text == "Entity")
            {
                var typeArg = genericName.TypeArgumentList.Arguments.FirstOrDefault();
                if (typeArg?.ToString() == modelName)
                {
                    hasEntityOfModel = true;
                }
            }
        }

        if (!hasEntityOfModel) return false;

        var lambdas = statement.DescendantNodes().OfType<SimpleLambdaExpressionSyntax>();
        foreach (var lambda in lambdas)
        {
            var memberAccesses = lambda.DescendantNodes().OfType<MemberAccessExpressionSyntax>();
            foreach (var memberAccess in memberAccesses)
            {
                if (memberAccess.Name.Identifier.Text == propertyName)
                {
                    referencesProperty = true;
                    break;
                }
            }
            if (referencesProperty) break;
        }

        return hasEntityOfModel && referencesProperty;
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