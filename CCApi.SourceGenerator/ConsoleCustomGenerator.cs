using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace CCApi.SourceGenerator;

[Generator]
public class ConsoleCustomGenerator : ISourceGenerator
{
    // IIncremental
    // public void Initialize(IncrementalGeneratorInitializationContext context)
    // {
    //     throw new NotImplementedException();
    // }


    public void Initialize(GeneratorInitializationContext context)
    {
    }

    public void Execute(GeneratorExecutionContext context)
    {
        var sourceText = SourceText.From($@"
        namespace GeneratedClass
        {{
            public class SeattleCompanies
            {{
                public string ForTheCloud => ""Microsoft"";
                public string ForTheTwoDayShipping => ""Amazon"";
                public string ForTheExpenses => ""Concur"";
            }}
        }}", Encoding.UTF8);
        context.AddSource("SeattleCompanies.cs", sourceText);
    }
}