using Microsoft.CodeAnalysis;

namespace Dappi.SourceGenerator.Models;

public class SourceModel
{
    public string ClassName { get; set; }
    public string ModelNamespace { get; set; }
    public string RootNamespace { get; set; }
    public List<PropertyInfo> PropertiesInfos { get; set; }
    public List<DappiAuthorizeInfo> AuthorizeAttributes { get; set; } = new();
}

public class PropertyInfo
{
    public string PropertyName { get; set; }
    public ITypeSymbol PropertyType { get; set; }
    public string PropertyForeignKey { get; set; }
    public List<string> PropertyAttributes { get; set; }
    public string GenericTypeName { get; set; }
}

public class DappiAuthorizeInfo
{
    public List<string>? Roles { get; set; }
    public List<string>? Methods { get; set; }
    public bool IsAuthenticated { get; set; } = true;
    public bool OnControllerLevel => Methods is null or {Count: 0};
}