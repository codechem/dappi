namespace Dappi.HeadlessCms.Interfaces
{
    public interface IAuditableEntity
    {
        DateTime? CreatedAtUtc { get; set; }

        DateTime? UpdatedAtUtc { get; set; }

        string? CreatedBy { get; set; }

        string? UpdatedBy { get; set; }
    }
}