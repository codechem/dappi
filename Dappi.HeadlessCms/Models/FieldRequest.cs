using System.ComponentModel.DataAnnotations;

namespace Dappi.HeadlessCms.Models;

public class FieldRequest
{
    [Required] 
    public string FieldName { get; set; } = null!;
    
    [Required] 
    public string FieldType { get; set; } = null!;
    
    public string? RelatedTo { get; set; }
    
    public bool IsRequired { get; set; } = false;
    
    public string? RelatedRelationName { get; set; }
    
    public string? Regex { get; set; }
    
    public int? MaxLength { get; set; }
    
    public int? MinLength { get; set; }
}