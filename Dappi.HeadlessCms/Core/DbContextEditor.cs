using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.EntityFrameworkCore;

namespace Dappi.HeadlessCms.Core;

public class DbContextEditor : IDisposable
{
    private Document _dbContextDocument;
    private readonly AdhocWorkspace _workspace;
    private readonly string _dbContextFilePath;
    private Project _project;

    public bool HasChanges { get; private set; }

    public DbContextEditor(string dbContextFilePath)
    {
        _dbContextFilePath = dbContextFilePath;
        var sourceCode = File.ReadAllText(dbContextFilePath);

        _workspace = new AdhocWorkspace();

        _project = _workspace.AddProject(nameof(DbContextEditor), LanguageNames.CSharp)
            .AddMetadataReference(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddMetadataReference(MetadataReference.CreateFromFile(typeof(DbContext).Assembly.Location));

        _dbContextDocument = _project.AddDocument("AppDbContext.cs", sourceCode);
    }

    public async Task AddDbSetAsync(DomainModelEntityInfo modelType)
    {
        var editor = await DocumentEditor.CreateAsync(_dbContextDocument);
        var root = await _dbContextDocument.GetSyntaxRootAsync();
        var classNode = root?.DescendantNodes().OfType<ClassDeclarationSyntax>()
            .FirstOrDefault(x => x.Identifier.Text.EndsWith("DbContext"));

        if (classNode == null)
            throw new InvalidOperationException("Class not found");

        var modelName = modelType.Name;
        var propertyName = $"{modelName}s";

        var existingDbSet = classNode.Members
            .OfType<PropertyDeclarationSyntax>()
            .FirstOrDefault(p =>
                p.Identifier.Text == propertyName &&
                p.Type is GenericNameSyntax generic &&
                generic.Identifier.Text == "DbSet" &&
                generic.TypeArgumentList.Arguments.FirstOrDefault()?.ToString() == modelName);

        if (existingDbSet == null)
        {
            var dbSetProperty = SyntaxFactory
                .PropertyDeclaration(
                    SyntaxFactory.GenericName("DbSet")
                        .WithTypeArgumentList(
                            SyntaxFactory.TypeArgumentList(
                                SyntaxFactory.SingletonSeparatedList<TypeSyntax>(
                                    SyntaxFactory.IdentifierName(modelName)))),
                    SyntaxFactory.Identifier(propertyName))
                .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
                .WithAccessorList(
                    SyntaxFactory.AccessorList(SyntaxFactory.List(new[]
                    {
                        SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                        SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                    })));

            editor.AddMember(classNode, dbSetProperty);
        }

        var updatedDoc = editor.GetChangedDocument();
        var updatedRoot = await updatedDoc.GetSyntaxRootAsync();
        var updatedUsings = updatedRoot?.DescendantNodes().OfType<UsingDirectiveSyntax>();
        var apiNamespace = modelType.Namespace;

        if (updatedUsings!.All(u => u.Name?.ToString() != apiNamespace))
        {
            var usingDirective = SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(apiNamespace!));
            updatedRoot = ((CompilationUnitSyntax)updatedRoot!).AddUsings(usingDirective);
            updatedDoc = updatedDoc.WithSyntaxRoot(updatedRoot);
        }

        _dbContextDocument = updatedDoc;
        HasChanges = true;
    }

    public async Task<IEnumerable<DomainModelEntityInfo>> GetUnregisteredDomainModelEntitiesAsync(
        IEnumerable<DomainModelEntityInfo> allDomainModelEntities)
    {
        var semanticModel = await _dbContextDocument.GetSemanticModelAsync();
        var root = await _dbContextDocument.GetSyntaxRootAsync();
        var propertyDeclarations = root?.DescendantNodes().OfType<PropertyDeclarationSyntax>();
        var registeredModelNames = new List<string>();

        if (propertyDeclarations is not null)
        {
            foreach (var propertyDeclaration in propertyDeclarations)
            {
                var type = (propertyDeclaration.Type as GenericNameSyntax)?.TypeArgumentList.Arguments[0];
                if (type is not null)
                {
                    registeredModelNames.Add(type.ToString());
                }
            }
        }

        return allDomainModelEntities.ExceptBy(registeredModelNames, x => x.Name);
    }

    public async Task SaveAsync()
    {
        var options = _workspace.Options;
        var formattedDoc = await Formatter.FormatAsync(_dbContextDocument, options);
        var finalCode = await formattedDoc.GetTextAsync();
        await File.WriteAllTextAsync(_dbContextFilePath, finalCode.ToString());
    }
    
    public void Dispose()
    {
        _workspace.Dispose();
        GC.SuppressFinalize(this);
    }
}