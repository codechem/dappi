using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Text;

namespace CCApi.WebApiExample.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AddFieldController : ControllerBase
    {
        private readonly string _entitiesFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "Entities");

        [HttpPost]
        public IActionResult AddField([FromBody] FieldRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.ModelName) || string.IsNullOrWhiteSpace(request.FieldName) || string.IsNullOrWhiteSpace(request.FieldType))
            {
                return BadRequest("Model name, field name, and field type must be provided.");
            }

            try
            {
                var modelFilePath = Path.Combine(_entitiesFolderPath, $"{request.ModelName}.cs");

                if (!System.IO.File.Exists(modelFilePath))
                {
                    return NotFound("Model class not found.");
                }

                var existingCode = System.IO.File.ReadAllText(modelFilePath);

                var updatedCode = AddFieldToClass(existingCode, request.FieldName, request.FieldType);

                System.IO.File.WriteAllText(modelFilePath, updatedCode);

                return Ok(new
                {
                    Message = $"Field '{request.FieldName}' of type '{request.FieldType}' added successfully to '{request.ModelName}' model.",
                    FilePath = modelFilePath
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        private string AddFieldToClass(string classCode, string fieldName, string fieldType)
        {
            
            var propertyCode = $"        public {fieldType} {fieldName} {{ get; set; }}";
            var insertPosition = classCode.LastIndexOf("    }", StringComparison.Ordinal); 
            var updatedCode = classCode.Insert(insertPosition, Environment.NewLine + propertyCode);

            return updatedCode;
        }
    }

    public class FieldRequest
    {
        public string ModelName { get; set; }
        public string FieldName { get; set; }
        public string FieldType { get; set; }
    }
}
