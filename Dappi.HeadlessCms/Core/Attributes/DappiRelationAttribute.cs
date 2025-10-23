using System.Text.Json.Serialization;

namespace Dappi.HeadlessCms.Core.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class DappiRelationAttribute(DappiRelationKind relationKind) : Attribute
{
    public DappiRelationKind RelationKind { get; set; } = relationKind;
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DappiRelationKind
{
    OneToOne,
    OneToMany,
    ManyToOne,
    ManyToMany,
}
