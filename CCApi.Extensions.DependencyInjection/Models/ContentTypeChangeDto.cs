public class ContentTypeChangeDto
{
    public int Id { get; set; }
    public string ModelName { get; set; }
    public Dictionary<string, string> Fields { get; set; }
    public string ModifiedBy { get; set; }
    public DateTimeOffset ModifiedAt { get; set; }
    public bool IsPublished { get; set; }
}