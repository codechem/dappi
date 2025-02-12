using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace CCApi.SourceGenerator.Generators;

[Generator]
public class ExtensionsGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterImplementationSourceOutput(
            context.CompilationProvider,
            (context, compilation) => GenerateExtensions(context, compilation)
        );
    }

    private void GenerateExtensions(SourceProductionContext context, Compilation compilation)
    {
        var rootNamespace = compilation.AssemblyName ?? "DefaultNamespace";
        var sourceText = SourceText.From($@"
using System.Text.RegularExpressions;
using System.Linq.Expressions;
using {rootNamespace}.Filtering;

namespace {rootNamespace}.Extensions;

public static class LinqExtensions
{{
    public static IQueryable<T> ApplyFiltering<T>(IQueryable<T> query, object filter)
    {{
        var filterProperties = filter.GetType().GetProperties()
            .Where(p => typeof(T).GetProperty(p.Name) != null);

        foreach (var property in filterProperties)
        {{
            var value = property.GetValue(filter);
            if (value == null) continue; // Skip null values

            // Build a dynamic filter expression
            var parameter = Expression.Parameter(typeof(T), ""x"");
            var propertyExpression = Expression.Property(parameter, property.Name);
            var constant = Expression.Constant(value);
            var equals = Expression.Equal(propertyExpression, constant);

            var lambda = Expression.Lambda<Func<T, bool>>(equals, parameter);
            query = query.Where(lambda);
        }}

        return query;
    }}

    public static IQueryable<T> ApplySorting<T>(IQueryable<T> query, string sortBy, SortDirection sortDirection)
    {{
        var parameter = Expression.Parameter(typeof(T), ""x"");
        var property = Expression.Property(parameter, sortBy);
        var lambda = Expression.Lambda(property, parameter);

        var methodName = sortDirection == SortDirection.Ascending ? ""OrderBy"" : ""OrderByDescending"";
        var method = typeof(Queryable).GetMethods()
            .First(m => m.Name == methodName && m.GetParameters().Length == 2)
            .MakeGenericMethod(typeof(T), property.Type);

        return (IQueryable<T>)method.Invoke(null, new object[] {{ query, lambda }});
    }}
}}

public static class PropertyCheckExtensions 
{{
    public static bool PropertyExists(string classCode, string fieldName, string fieldType)
    {{
        var propertyPattern = $@""\bpublic\s+{{Regex.Escape(fieldType)}}\s+{{Regex.Escape(fieldName)}}\s*\{{{{"";
        return Regex.IsMatch(classCode, propertyPattern, RegexOptions.IgnoreCase);
    }}
}}
", Encoding.UTF8);
        context.AddSource("DappiExtensions.cs", sourceText);
    }
}