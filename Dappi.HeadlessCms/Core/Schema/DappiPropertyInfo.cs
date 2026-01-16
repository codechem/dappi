using System.Text.Json.Serialization;
using Dappi.HeadlessCms.Core.Attributes;

namespace Dappi.HeadlessCms.Core.Schema;

[JsonConverter(typeof(PropertyInfoJsonConverter))]
public class DappiPropertyInfo
{
    public string Type { get; set; } = null!;
    public bool Required { get; set; }
}

public class StringProperty : DappiPropertyInfo
{
    public int? MaxLength { get; set; }
    public int? MinLength { get; set; }
    public string? Regex { get; set; }
}

public class NumberProperty : DappiPropertyInfo
{
    public double? MinValue { get; set; }
    public double? MaxValue { get; set; }
}

public class EnumProperty : DappiPropertyInfo
{
    public List<string> Values { get; set; } = new();
}

public class MediaProperty : DappiPropertyInfo
{
    public List<string> AllowedTypes { get; set; } = new();
}

public class RelationProperty : DappiPropertyInfo
{
    public DappiRelationKind RelationType { get; set; }
    public string Target { get; set; } = null!;
    public string? InversedBy { get; set; }
    public string? MappedBy { get; set; }
}

public static class StringExtensions
{
    public static string ToCamelCase(this string str) =>
        string.IsNullOrEmpty(str) ? str : char.ToLowerInvariant(str[0]) + str.Substring(1);
}