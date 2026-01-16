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
        public bool NoPastDates { get; set; } = false;
        public bool HasIndex { get; set; } = false;
        public string? MinLength { get; set; }
        public string? MaxLength { get; set; }
        public string? MinValue { get; set; }
        public string? MaxValue { get; set; }
    }
}