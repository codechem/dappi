using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace CCApi.SourceGenerator.Generators;

[Generator]
public class HelperDtosGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
    }

    public void Execute(GeneratorExecutionContext context)
    {
        var sourceText = SourceText.From($@"
namespace CC.ApiGen.HelperDtos;

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