public class ContentTypeChange
{
    public int Id { get; set; }
    public string ModelName { get; set; }
    public string Fields { get; set; }
    public string ModifiedBy { get; set; }
    public DateTimeOffset ModifiedAt { get; set; }
    public bool IsPublished { get; set; }
}