using System.ComponentModel.DataAnnotations;

namespace CCApi.Extensions.DependencyInjection.Models;

public class FieldRequest
{
    [Required]
    public string FieldName { get; set; } = null!;

    [Required]
    public string FieldType { get; set; } = null!;

    public string? RelatedTo { get; set; }

    public bool IsRequired { get; set; } = false;

    public string? RelatedRelationName { get; set; }
}