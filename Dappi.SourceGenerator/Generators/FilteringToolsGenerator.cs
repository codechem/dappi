using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Dappi.SourceGenerator.Generators;

[Generator]
public class FilteringToolsGenerator : IIncrementalGenerator
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
namespace {rootNamespace}.Filtering;

public interface IPagingFilter
{{
    int Limit {{ get; set; }}
    int Offset {{ get; set; }}
}}

public interface ISortingFilter
{{
    string SortBy {{ get; set; }}
    SortDirection SortDirection {{ get; set; }}
}}

public interface ISearchFilter
{{
    string? SearchTerm {{ get; set; }}
}}

public interface IGenericFilter : ISortingFilter, IPagingFilter, ISearchFilter
{{
}}

public enum SortDirection
{{
    Ascending = 1,
    Descending = -1
}}

public class PagingFilter : IGenericFilter
{{
    public int Limit {{ get; set; }} = 10;
    public int Offset {{ get; set; }}

    public string SortBy {{ get; set; }} = ""Id"";
    public SortDirection SortDirection {{ get; set; }} = SortDirection.Ascending;

    public string? SearchTerm {{ get; set; }}
}}
", Encoding.UTF8);
        context.AddSource("CCFilter.cs", sourceText);
    }
}