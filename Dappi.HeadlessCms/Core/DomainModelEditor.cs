using Dappi.HeadlessCms.Core.Schema;
using Dappi.HeadlessCms.Core.SyntaxVisitors;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Dappi.HeadlessCms.Core;

public class DomainModelEditor(string domainModelFolderPath)
{
    public async Task<DomainModelEntityInfo[]> GetDomainModelEntityInfos()
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

    public void AddDomainModelEntityInfo(DomainModelEntityInfo entityInfo)
    {
        
    }
    
}