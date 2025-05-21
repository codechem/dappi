namespace CCApi.Extensions.DependencyInjection.Models;

public class ContentTypeChangeDto
{
    public int Id { get; set; }
    public required string ModelName { get; set; }
    public required Dictionary<string, string> Fields { get; set; }
    public required Guid ModifiedBy { get; set; }
    public required DateTimeOffset ModifiedAt { get; set; }
    public required bool IsPublished { get; set; }
}