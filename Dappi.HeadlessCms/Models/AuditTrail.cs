using System.ComponentModel.DataAnnotations;

namespace Dappi.HeadlessCms.Models
{
    public class AuditTrail
    {
        [Key]
        public required Guid Id { get; set; }

        public string? UserId { get; set; }
        
        public TrailType TrailType { get; set; }

        public DateTime DateUtc { get; set; }

        public required string EntityName { get; set; }

        public string EntityId { get; set; }

        public Dictionary<string, object?> OldValues { get; set; } = [];

        public Dictionary<string, object?> NewValues { get; set; } = [];

        public List<string> ChangedColumns { get; set; } = [];
    }
}