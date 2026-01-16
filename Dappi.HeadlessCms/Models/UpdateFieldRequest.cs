using System.ComponentModel.DataAnnotations;

namespace Dappi.HeadlessCms.Models;

public class UpdateFieldRequest
{
    [Required]
    public string OldFieldName { get; set; } = null!;
    
    [Required]
    public string NewFieldName { get; set; } = null!;

    public bool IsRequired { get; set; } = false;

    public string? Regex { get; set; }

    public bool HasIndex { get; set; } = false;

    public bool NoPastDates { get; set; } = false;
    
    public int? MinLength { get; set; }
    
    public int? MaxLength { get; set; }
    
    public double? MinValue { get; set; }
    
    public double? MaxValue { get; set; }
}
