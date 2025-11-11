using System.ComponentModel.DataAnnotations;

namespace Dappi.HeadlessCms.Models;
    
public class CreateEnumRequest
{
    [Required]
    [RegularExpression(@"^[A-Z][a-zA-Z0-9]*$", ErrorMessage = "Enum name must start with uppercase letter and contain only alphanumeric characters")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MinLength(1, ErrorMessage = "At least one enum value is required")]
    public List<EnumValueRequest> Values { get; set; } = new();
}

public class UpdateEnumRequest
{
    [Required]
    [MinLength(1, ErrorMessage = "At least one enum value is required")]
    public List<EnumValueRequest> Values { get; set; } = new();
}

public class EnumValueRequest
{
    [Required]
    [RegularExpression(@"^[A-Z][a-zA-Z0-9]*$", ErrorMessage = "Enum value name must start with uppercase letter and contain only alphanumeric characters")]
    public string Name { get; set; } = string.Empty;

    [Range(0, int.MaxValue, ErrorMessage = "Enum value must be a non-negative integer")]
    public int Value { get; set; }
}
