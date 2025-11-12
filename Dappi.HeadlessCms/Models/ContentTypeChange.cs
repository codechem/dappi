using Dappi.HeadlessCms.Enums;

namespace Dappi.HeadlessCms.Models;

public class ContentTypeChange
{
    public int Id { get; init; }
    public required string ModelName { get; init; }
    public string Fields { get; set; } = string.Empty;
    public Guid ModifiedBy { get; init; } = Guid.Empty;
    public DateTimeOffset ModifiedAt { get; set; } = DateTimeOffset.UtcNow;
    public ContentTypeState State { get; set; }
}