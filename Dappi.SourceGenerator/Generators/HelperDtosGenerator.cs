using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Dappi.SourceGenerator.Generators;

[Generator]
public class HelperDtosGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterImplementationSourceOutput(
            context.CompilationProvider,
            (context, compilation) => GenerateFiltering(context, compilation)
        );
    }

    private void GenerateFiltering(SourceProductionContext context, Compilation compilation)
    {
        var rootNamespace = compilation.AssemblyName ?? "DefaultNamespace";
        var sourceText = SourceText.From($@"
namespace {rootNamespace}.HelperDtos;

public class ListResponseDTO<T>
{{
    public long Total {{ get; set; }}
    public long Offset {{ get; set; }}
    public long Limit {{ get; set; }}
    public IEnumerable<T> Data {{ get; set; }}
}}
", Encoding.UTF8);
        context.AddSource("HelperDtos.cs", sourceText);
    }
}