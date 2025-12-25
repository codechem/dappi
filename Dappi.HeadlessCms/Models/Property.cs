using Dappi.HeadlessCms.Core.Attributes;

namespace Dappi.HeadlessCms.Models
{
    public record Property
    {
        public string DomainModel { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public bool IsRequired { get; set; } = false;
        public DappiRelationKind? RelationKind { get; set; } = null;
        public string? RelatedDomainModel { get; set; } = null;
        public string? Regex { get; set; } = null;
        public string? MinLength { get; set; }
        public string? MaxLength { get; set; }
    }
}