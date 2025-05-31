using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Dappi.HeadlessCms.Core;

public class DomainModelEditor : IDisposable
{
    private readonly AdhocWorkspace _workspace;
    private readonly IEnumerable<Document> _documents; 
    private bool _disposed;

    public DomainModelEditor(string domainModelFilePath)
    {
        var modelFiles = Directory.GetFiles(domainModelFilePath, "*.cs");

        _workspace = new AdhocWorkspace();

        var project = _workspace.AddProject(nameof(DomainModelEditor), LanguageNames.CSharp)
            .AddMetadataReference(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));

        foreach (var file in modelFiles)
        {
            var code = File.ReadAllText(file);
            var document = project.AddDocument(Path.GetFileName(file), code);
            project = document.Project;
        }

        _documents = project.Documents;
    }

    public async Task<List<DomainModelEntityInfo>> GetDomainModelEntitiesAsync()
    {
        var modelInfos = new List<DomainModelEntityInfo>();

        foreach (var document in _documents)
        {
            var semanticModel = await document.GetSemanticModelAsync();
            var root = await document.GetSyntaxRootAsync();
            if (semanticModel == null || root == null) continue;

            var classDecl = root.DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault();
            if (classDecl == null) continue;

            var symbol = semanticModel.GetDeclaredSymbol(classDecl) as INamedTypeSymbol;
            if (symbol == null) continue;

            var attributeShortName = nameof(CCControllerAttribute).Replace("Attribute", "");
            var hasCCControllerAttribute = symbol
                .GetAttributes()
                .Any(attr => attr.AttributeClass?.Name == attributeShortName);

            if (!hasCCControllerAttribute) continue;

            var namespaceName = symbol.ContainingNamespace?.ToDisplayString() ?? string.Empty;
            var propDecl = root.DescendantNodes()
                .OfType<PropertyDeclarationSyntax>();

            var properties = propDecl.Select(prop => new PropertyInfo
            {
                Name = prop.Identifier.ToString(),
                Type = prop.Type.ToString()
            });

            modelInfos.Add(new DomainModelEntityInfo
            {
                Name = symbol.Name,
                Namespace = namespaceName,
                Properties = properties.ToList(),
            });
        }

        return modelInfos;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _workspace.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}