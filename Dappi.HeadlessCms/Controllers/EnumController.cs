using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using CCApi.Extensions.DependencyInjection.Interfaces;

namespace CCApi.Extensions.DependencyInjection.Controllers;

[ApiController]
[Route("api/enum-manager")]
public class EnumsController : ControllerBase
{
    private readonly IEnumService _enumService;

    public EnumsController(IEnumService enumService)
    {
        _enumService = enumService;
    }

    [HttpGet("getAll")]
    public async Task<IActionResult> GetAllEnums()
    {
        try
        {
            var enums = await _enumService.GetAllEnumsAsync();
            return Ok(enums);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Failed to retrieve enums", error = ex.Message });
        }
    }

    [HttpGet("{enumName}")]
    public async Task<IActionResult> GetEnum(string enumName)
    {
        try
        {
            var enumData = await _enumService.GetEnumAsync(enumName);
            if (enumData == null)
            {
                return NotFound(new { message = $"Enum '{enumName}' not found" });
            }
            return Ok(enumData);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Failed to retrieve enum", error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateEnum([FromBody] CreateEnumRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var result = await _enumService.CreateEnumAsync(request.Name, request.Values);
            if (!result.Success)
            {
                return BadRequest(new { message = result.ErrorMessage });
            }

            return CreatedAtAction(nameof(GetEnum), new { enumName = request.Name }, result.Data);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Failed to create enum", error = ex.Message });
        }
    }

    [HttpPut("{enumName}")]
    public async Task<IActionResult> UpdateEnum(string enumName, [FromBody] UpdateEnumRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var result = await _enumService.UpdateEnumAsync(enumName, request.Values);
            if (!result.Success)
            {
                return BadRequest(new { message = result.ErrorMessage });
            }

            return Ok(result.Data);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Failed to update enum", error = ex.Message });
        }
    }

    [HttpDelete("{enumName}")]
    public async Task<IActionResult> DeleteEnum(string enumName)
    {
        try
        {
            var result = await _enumService.DeleteEnumAsync(enumName);
            if (!result.Success)
            {
                return BadRequest(new { message = result.ErrorMessage });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Failed to delete enum", error = ex.Message });
        }
    }

    [HttpPost("regenerate")]
    public async Task<IActionResult> RegenerateEnumFiles()
    {
        try
        {
            await _enumService.RegenerateAllEnumFilesAsync();
            return Ok(new { message = "Enum files regenerated successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Failed to regenerate enum files", error = ex.Message });
        }
    }
}

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

