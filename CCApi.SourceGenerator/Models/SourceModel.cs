using Microsoft.CodeAnalysis;

namespace CCApi.SourceGenerator.Models;

public class SourceModel
{
    public string ClassName { get; set; }
    public string ModelNamespace { get; set; }
    public string RootNamespace { get; set; }
    public List<PropertyInfo> PropertiesInfos { get; set; }
}

public class PropertyInfo
{
    public string PropertyName { get; set; }
    public ITypeSymbol PropertyType { get; set; }
    public string PropertyForeignKey { get; set; }
    public List<string> PropertyAttributes { get; set; }
}