using Microsoft.CodeAnalysis;

namespace Dappi.SourceGenerator.Models;

public class SourceModel
{
    public string ClassName { get; set; } = string.Empty;
    public string ModelNamespace { get; set; } = string.Empty;
    public string RootNamespace { get; set; }  = string.Empty;
    public List<PropertyInfo> PropertiesInfos { get; set; } = [];
    public List<DappiAuthorizeInfo> AuthorizeAttributes { get; set; } = [];
}

public class PropertyInfo
{
    public string PropertyName { get; set; } = string.Empty;
    public ITypeSymbol PropertyType { get; set; } = null!;
    public string PropertyForeignKey { get; set; } = string.Empty;
    public List<string> PropertyAttributes { get; set; } = [];
    public string GenericTypeName { get; set; } = string.Empty;
}

public class DappiAuthorizeInfo
{
    public List<string> Roles { get; set; } = [];
    public List<string> Methods { get; set; } = [];
    public bool IsAuthenticated { get; set; } = true;
    public bool OnControllerLevel => Methods is null or {Count: 0};
}