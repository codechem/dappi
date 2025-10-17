namespace Dappi.HeadlessCms.Models
{
    public abstract class AuditableEntity
    { 
        public DateTime? CreatedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }
    }
}