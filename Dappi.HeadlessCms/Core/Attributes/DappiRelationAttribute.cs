using System.Text.Json.Serialization;

namespace Dappi.HeadlessCms.Core.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class DappiRelationAttribute(DappiRelationKind relationKind, Type inverseSide) : Attribute
{
    public static readonly string ShortName = nameof(DappiRelationAttribute).Replace("Attribute","");
    public DappiRelationKind RelationKind { get; set; } = relationKind;
    public Type? InverseSide { get; set; } = inverseSide;
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DappiRelationKind
{
    OneToOne,
    OneToMany,
    ManyToOne,
    ManyToMany,
}
