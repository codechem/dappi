namespace CCApi.Extensions.DependencyInjection.Models;

public class FieldRequest
{
    public string FieldName { get; set; }
    public string FieldType { get; set; }
    public string RelatedTo { get; set; }
    public bool IsRequired { get; set; } = false;
    public string RelatedRelationName { get; set; }
}