namespace Dappi.HeadlessCms.Core.Schema;

public class DomainModelEntityInfo
{
    public required string Name { get; init; }
    public required string Namespace { get; init; }
    public Dictionary<string, DappiPropertyInfo> Properties { get; set; } = [];
}