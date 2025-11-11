using Dappi.HeadlessCms.Core;
using Dappi.HeadlessCms.Extensions;
using Dappi.HeadlessCms.Models;
using Microsoft.AspNetCore.Mvc;

namespace Dappi.HeadlessCms.Controllers
{
    [ApiExplorerSettings(GroupName = "Toolkit")]
    [Route("api/enums")]
    [ApiController]
    public class EnumController : ControllerBase
    {
        private readonly EnumEditor _enumEditor;
        private readonly DomainModelEditor _domainModelEditor;
        
        private readonly string _enumsFolderPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "Enums"
        );
        
        public EnumController(EnumEditor enumEditor, DomainModelEditor domainModelEditor)
        {
            _enumEditor = enumEditor;
            _domainModelEditor = domainModelEditor;
            if (!Directory.Exists(_enumsFolderPath))
            {
                Directory.CreateDirectory(_enumsFolderPath);
            }
        }

        [HttpPost("{enumName}")]
        public async Task<IActionResult> CreateEnum([FromRoute] string enumName)
        {
            if (string.IsNullOrWhiteSpace(enumName))
            {
                return BadRequest("Enum name must be provided.");
            }

            if (!enumName.IsValidClassNameOrPropertyName())
            {
                return BadRequest($"Enum name {enumName} is invalid");
            }

            var enumNames = DirectoryUtils.GetClassNamesFromDirectory(_enumsFolderPath);
            if (enumNames.Contains(enumName))
            {
                return BadRequest($"A enum with the name '{enumName}' already exists.");
            }

            try
            {
                _enumEditor.CreateEnum(enumName);
                await _enumEditor.SaveAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPut("{enumName}")]
        public async Task<IActionResult> UpdateEnum([FromRoute] string enumName, [FromBody] EnumFieldRequest request)
        {
            if (string.IsNullOrWhiteSpace(enumName))
            {
                return BadRequest("Enum name must be provided.");
            }

            var enumNames = DirectoryUtils.GetClassNamesFromDirectory(_enumsFolderPath);
            if (!enumNames.Contains(enumName))
            {
                return BadRequest($"A enum with the name '{enumName}' does not exist.");
            }

            try
            {
                _enumEditor.UpdateEnum(enumName, request);
                await _enumEditor.SaveAsync();
                return Ok();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpDelete("{enumName}")]
        public async Task<IActionResult> DeleteEnum([FromRoute] string enumName)
        {
            if (string.IsNullOrWhiteSpace(enumName))
            {
                return BadRequest("Enum name must be provided.");
            }

            var enumNames = DirectoryUtils.GetClassNamesFromDirectory(_enumsFolderPath);
            if (!enumNames.Contains(enumName))
            {
                return BadRequest($"A enum with the name '{enumName}' does not exist.");
            }
            try
            {
                var filePath = Path.Combine(_enumsFolderPath, $"{enumName}.cs");
                var models = await _domainModelEditor.GetDomainModelEntityInfosAsync();
                System.IO.File.Delete(filePath);
                foreach (var model in models)
                {
                    _domainModelEditor.RemoveEnumProperty(model.Name, enumName);
                    if (!Directory.EnumerateFiles(_enumsFolderPath, "*.cs", SearchOption.AllDirectories).Any())
                    {
                        _domainModelEditor.UpdateUsings(model);
                    }
                }
                await _domainModelEditor.SaveAsync();
                
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}