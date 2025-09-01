using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Dappi.HeadlessCms.Core;
using Dappi.HeadlessCms.Database;
using Dappi.HeadlessCms.Extensions;
using Dappi.HeadlessCms.Interfaces;
using Dappi.HeadlessCms.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Dappi.HeadlessCms.Controllers
{
    [ApiExplorerSettings(GroupName = "Toolkit")]
    [Route("api/models")]
    [ApiController]
    public class ModelsController : ControllerBase
    {
        private readonly ICurrentSessionProvider _currentSessionProvider;
        private readonly DappiDbContext _dbContext;

        private readonly string _entitiesFolderPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "Entities"
        );

        private readonly string _controllersFolderPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "Controllers"
        );

        public ModelsController(
            IDbContextAccessor dappiDbContextAccessor,
            ICurrentSessionProvider currentSessionProvider)
        {
            _currentSessionProvider = currentSessionProvider;
            _dbContext = dappiDbContextAccessor.DbContext;

            if (!Directory.Exists(_entitiesFolderPath))
            {
                Directory.CreateDirectory(_entitiesFolderPath);
            }
        }

        [HttpGet]
        public IActionResult GetAllModels()
        {
            try
            {
                if (!Directory.Exists(_entitiesFolderPath))
                {
                    return NotFound("Entities directory not found.");
                }

                var modelNames = DirectoryUtils.GetClassNamesFromDirectory(_entitiesFolderPath);
                return Ok(modelNames);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateModel([FromBody] ModelRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.ModelName))
            {
                return BadRequest("Model name must be provided.");
            }

            if (!request.ModelName.IsValidClassNameOrPropertyName())
            {
                return BadRequest("Model name is invalid");
            }

            var modelNames = DirectoryUtils.GetClassNamesFromDirectory(_entitiesFolderPath);
            if (modelNames.Contains(request.ModelName))
            {
                return BadRequest($"A model with the name '{request.ModelName}' already exists.");
            }

            try
            {
                var modelType = CreateModel(request.ModelName);
                if (modelType == null)
                {
                    return BadRequest("Failed to create dynamic model.");
                }

                var fileName = $"{modelType.Name}.cs";
                var filePath = Path.Combine(_entitiesFolderPath, fileName);
                var classCode = GenerateClassCode(modelType);

                System.IO.File.WriteAllText(filePath, classCode);

                await AddContentTypeChangeAsync(
                    request.ModelName,
                    new Dictionary<string, string>() { { "Id", "Guid" } }
                );

                return Ok(
                    new { Message = $"Model class '{modelType.Name}' created successfully.", FilePath = filePath, }
                );
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpDelete("{modelName}")]
        public async Task<IActionResult> DeleteModel(string modelName)
        {
            if (string.IsNullOrWhiteSpace(modelName))
            {
                return BadRequest("Model name must be provided.");
            }

            var modelFilePath = Path.Combine(_entitiesFolderPath, $"{modelName}.cs");

            try
            {
                if (!System.IO.File.Exists(modelFilePath))
                {
                    return NotFound("Model file not found.");
                }

                System.IO.File.Delete(modelFilePath);

                var dbContextFilePath = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "Data",
                    "AppDbContext.cs"
                );

                string dbContextContent = System.IO.File.ReadAllText(dbContextFilePath);
                string pattern =
                    $@"\s*public\s+DbSet<{modelName}>\s+{modelName}s\s+\{{\s+get;\s+set;\s+\}}";
                dbContextContent = Regex.Replace(
                    dbContextContent,
                    pattern,
                    string.Empty,
                    RegexOptions.Multiline
                );

                System.IO.File.WriteAllText(dbContextFilePath, dbContextContent);

                var controllerFilePath = Path.Combine(
                    _controllersFolderPath,
                    $"{modelName}Controller.cs"
                );

                if (System.IO.File.Exists(controllerFilePath))
                {
                    System.IO.File.Delete(controllerFilePath);
                }

                await AddContentTypeChangeAsync(modelName, new Dictionary<string, string>());

                return Ok(
                    new { Message = $"Model '{modelName}' deleted successfully.", FilePath = modelFilePath, }
                );
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPut("{modelName}")]
        public async Task<IActionResult> AddField(string modelName, [FromBody] FieldRequest request)
        {
            if (string.IsNullOrEmpty(modelName))
            {
                return BadRequest("Model name must be provided.");
            }

            if (!request.FieldName.IsValidClassNameOrPropertyName())
            {
                return BadRequest($"Property name {request.FieldName} is invalid");
            }

            try
            {
                var modelFilePath = Path.Combine(_entitiesFolderPath, $"{modelName}.cs");
                if (!System.IO.File.Exists(modelFilePath))
                {
                    return NotFound("Model class not found.");
                }

                var fieldDict = new Dictionary<string, string> { { request.FieldName, request.FieldType } };
                var existingCode = await System.IO.File.ReadAllTextAsync(modelFilePath);

                if (PropertyCheckUtils.PropertyNameExists(existingCode, request.FieldName))
                {
                    return BadRequest($"Property {request.FieldName} name already exists in {modelFilePath}.");
                }

                if (!string.IsNullOrEmpty(request.RelatedTo))
                {
                    var modelRelatedToFilePath = Path.Combine(_entitiesFolderPath, $"{request.RelatedTo}.cs");
                    var existingRelatedToCode = await System.IO.File.ReadAllTextAsync(modelRelatedToFilePath);

                    switch (request.FieldType)
                    {
                        case "OneToOne":
                            {
                                var foreignKeyName = $"{request.FieldName}Id";
                                var updatedCode = AddFieldToClass(
                                    existingCode,
                                    foreignKeyName,
                                    $"Guid{(!request.IsRequired ? "?" : "")}",
                                    "",
                                    request.IsRequired
                                );
                                System.IO.File.WriteAllText(modelFilePath, updatedCode);

                                existingCode = System.IO.File.ReadAllText(modelFilePath);
                                updatedCode = AddFieldToClass(
                                    existingCode,
                                    request.FieldName,
                                    $"{request.RelatedTo}{(!request.IsRequired ? "?" : "")}",
                                    "",
                                    request.IsRequired
                                );
                                System.IO.File.WriteAllText(modelFilePath, updatedCode);

                                var relatedToCode = AddFieldToClass(
                                    existingRelatedToCode,
                                    request.RelatedRelationName ?? request.FieldName,
                                    $"{modelName}{(!request.IsRequired ? "?" : "")}",
                                    "",
                                    request.IsRequired
                                );
                                System.IO.File.WriteAllText(modelRelatedToFilePath, relatedToCode);

                                UpdateDbContextWithRelationship(modelName, request.RelatedTo, "OneToOne",
                                    request.FieldName,
                                    request.RelatedRelationName ?? request.FieldName);

                                fieldDict.Add(foreignKeyName, $"Guid{(!request.IsRequired ? "?" : "")}");
                                break;
                            }
                        case "OneToMany":
                            {
                                var foreignKeyName = $"{request.RelatedRelationName ?? modelName}Id";
                                var updatedCode = AddFieldToClass(
                                    existingCode,
                                    request.FieldName,
                                    $"ICollection<{request.RelatedTo}{(!request.IsRequired ? "?" : "")}>",
                                    $"{request.RelatedTo}{(!request.IsRequired ? "?" : "")}",
                                    request.IsRequired
                                );

                                System.IO.File.WriteAllText(modelFilePath, updatedCode);
                                
                                var relatedToCode = AddFieldToClass(
                                    existingRelatedToCode,
                                    foreignKeyName,
                                    $"Guid{(!request.IsRequired ? "?" : "")}",
                                    "",
                                    request.IsRequired
                                );
                                System.IO.File.WriteAllText(modelRelatedToFilePath, relatedToCode);

                                existingRelatedToCode = System.IO.File.ReadAllText(modelRelatedToFilePath);
                                relatedToCode = AddFieldToClass(
                                    existingRelatedToCode,
                                    request.RelatedRelationName ?? modelName,
                                    $"{modelName}{(!request.IsRequired ? "?" : "")}",
                                    "",
                                    request.IsRequired
                                );
                                System.IO.File.WriteAllText(modelRelatedToFilePath, relatedToCode);
                                
                                UpdateDbContextWithRelationship(modelName, request.RelatedTo, "OneToMany",
                                    request.FieldName,
                                    request.RelatedRelationName ?? modelName);
                                
                                fieldDict[request.FieldName] =
                                    $"ICollection<{request.RelatedTo}{(!request.IsRequired ? "?" : "")}>";

                                break;
                            }
                        case "ManyToOne":
                            {
                                var foreignKeyName = $"{request.FieldName}Id";
                                var updatedCode = AddFieldToClass(
                                    existingCode,
                                    foreignKeyName,
                                    $"Guid{(!request.IsRequired ? "?" : "")}",
                                    "",
                                    request.IsRequired
                                );
                                System.IO.File.WriteAllText(modelFilePath, updatedCode);

                                existingCode = System.IO.File.ReadAllText(modelFilePath);
                                updatedCode = AddFieldToClass(
                                    existingCode,
                                    request.FieldName,
                                    $"{request.RelatedTo}{(!request.IsRequired ? "?" : "")}",
                                    "",
                                    request.IsRequired
                                );
                                System.IO.File.WriteAllText(modelFilePath, updatedCode);

                                var relatedToCode = AddFieldToClass(
                                    existingRelatedToCode,
                                    request.RelatedRelationName ?? $"{modelName}s",
                                    $"ICollection<{modelName}{(!request.IsRequired ? "?" : "")}>",
                                    $"{modelName}{(!request.IsRequired ? "?" : "")}",
                                    request.IsRequired
                                );
                                System.IO.File.WriteAllText(modelRelatedToFilePath, relatedToCode);

                                UpdateDbContextWithRelationship(modelName, request.RelatedTo, "ManyToOne",
                                    request.FieldName,
                                    request.RelatedRelationName ?? $"{modelName}s");

                                fieldDict.Add(foreignKeyName, $"Guid{(!request.IsRequired ? "?" : "")}");
                                break;
                            }
                        case "ManyToMany":
                            {
                                var updatedCode = AddFieldToClass(
                                    existingCode,
                                    request.FieldName,
                                    $"ICollection<{request.RelatedTo}{(!request.IsRequired ? "?" : "")}>",
                                    $"{request.RelatedTo}{(!request.IsRequired ? "?" : "")}",
                                    request.IsRequired
                                );
                                System.IO.File.WriteAllText(modelFilePath, updatedCode);

                                var relatedToCode = AddFieldToClass(
                                    existingRelatedToCode,
                                    request.RelatedRelationName ?? $"{modelName}s",
                                    $"ICollection<{modelName}{(!request.IsRequired ? "?" : "")}>",
                                    $"{modelName}{(!request.IsRequired ? "?" : "")}",
                                    request.IsRequired
                                );
                                System.IO.File.WriteAllText(modelRelatedToFilePath, relatedToCode);

                                UpdateDbContextWithRelationship(modelName, request.RelatedTo, "ManyToMany",
                                    request.FieldName,
                                    request.RelatedRelationName ?? $"{modelName}s");

                                fieldDict[request.FieldName] =
                                    $"ICollection<{request.RelatedTo}{(!request.IsRequired ? "?" : "")}>";
                                break;
                            }
                    }
                }
                else
                {
                    var updatedCode = AddFieldToClass(
                        existingCode,
                        request.FieldName,
                        $"{request.FieldType}{(!request.IsRequired ? "?" : "")}",
                        "",
                        request.IsRequired
                    );

                    System.IO.File.WriteAllText(modelFilePath, updatedCode);
                }

                await UpdateContentTypeChangeFieldsAsync(modelName, fieldDict);

                return Ok(
                    new
                    {
                        Message =
                            $"Field '{request.FieldName}' of type '{request.FieldType}' added successfully to '{modelName}' model.",
                        FilePath = modelFilePath,
                    }
                );
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
        
        [HttpGet("fields/{modelName}")]
        public IActionResult GetModelFields(string modelName)
        {
            var modelFilePath = Path.Combine(
                Path.Combine(Directory.GetCurrentDirectory(), "Entities"),
                $"{modelName}.cs"
            );
            if (!System.IO.File.Exists(modelFilePath))
            {
                return NotFound($"Model '{{modelName}}' not found.");
            }

            var modelCode = System.IO.File.ReadAllText(modelFilePath);
            var fieldData = ExtractFieldsFromModel(modelCode);

            return Ok(fieldData);
        }

        private async Task AddContentTypeChangeAsync(
            string modelName,
            Dictionary<string, string> fields
        )
        {
            var contentTypeChange = new ContentTypeChange()
            {
                ModelName = modelName,
                Fields = JsonSerializer.Serialize(fields),
                ModifiedBy = _currentSessionProvider.GetCurrentUserId() ?? Guid.Empty,
            };

            _dbContext.ContentTypeChanges.Add(contentTypeChange);

            await _dbContext.SaveChangesAsync();
        }

        private async Task UpdateContentTypeChangeFieldsAsync(
            string modelName,
            Dictionary<string, string> newFields
        )
        {
            try
            {
                var contentTypeChangeForModel = await _dbContext.ContentTypeChanges
                    .Where(ctc => ctc.ModelName == modelName && !ctc.IsPublished)
                    .OrderByDescending(ctc => ctc.ModifiedAt)
                    .FirstOrDefaultAsync();

                if (contentTypeChangeForModel is not null)
                {
                    var oldFields =
                        JsonSerializer.Deserialize<Dictionary<string, string>>(contentTypeChangeForModel.Fields);
                    foreach (var kvp in newFields)
                    {
                        oldFields?.Add(kvp.Key, kvp.Value);
                    }
            
                    contentTypeChangeForModel.ModifiedAt = DateTimeOffset.UtcNow;
                    contentTypeChangeForModel.Fields = JsonSerializer.Serialize(oldFields);
                }
                else
                {
                    contentTypeChangeForModel = new ContentTypeChange()
                    {
                        ModelName = modelName, Fields = JsonSerializer.Serialize(newFields),
                    };
            
                    _dbContext.ContentTypeChanges.Add(contentTypeChangeForModel);
                }
            
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating ContentTypeChanges: {ex.Message}");
                throw;
            }
        }

        private void UpdateDbContextWithRelationship(string modelName, string relatedTo, string relationshipType,
            string propertyName, string? relatedPropertyName = null)
        {
            var dbContextFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "AppDbContext.cs");

            if (!System.IO.File.Exists(dbContextFilePath))
            {
                throw new FileNotFoundException($"DbContext file not found at {dbContextFilePath}");
            }

            var dbContextContent = System.IO.File.ReadAllText(dbContextFilePath);
            var configCode = GenerateRelationshipConfiguration(modelName, relatedTo, relationshipType, propertyName,
                relatedPropertyName);

            var onModelCreatingIndex =
                dbContextContent.IndexOf("protected override void OnModelCreating(ModelBuilder modelBuilder)", StringComparison.InvariantCulture);

            if (onModelCreatingIndex == -1)
            {
                var lastClosingBrace = dbContextContent.LastIndexOf("}", StringComparison.InvariantCulture);
                var onModelCreatingMethod = $@"
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {{
{configCode}

        base.OnModelCreating(modelBuilder);
    }}
";
                dbContextContent = dbContextContent.Insert(lastClosingBrace, onModelCreatingMethod);
            }
            else
            {
                const string baseCall = "base.OnModelCreating(modelBuilder);";
                var baseCallIndex = dbContextContent.IndexOf(baseCall, onModelCreatingIndex, StringComparison.InvariantCulture);

                if (baseCallIndex == -1)
                {
                    var methodEndPosition = FindMethodEndPosition(dbContextContent, onModelCreatingIndex);
                    var configCodeWithBase = $@"
{configCode}

        base.OnModelCreating(modelBuilder);
";
                    dbContextContent = dbContextContent.Insert(methodEndPosition, configCodeWithBase);
                }
                else
                {
                    var configCodeWithSpacing = $@"
{configCode}
";
                    dbContextContent = dbContextContent.Insert(baseCallIndex, configCodeWithSpacing);
                }
            }

            System.IO.File.WriteAllText(dbContextFilePath, dbContextContent);
        }

        private static string GenerateRelationshipConfiguration(string modelName, string relatedTo, string relationshipType,
            string propertyName, string? relatedPropertyName = null)
        {
            return relationshipType.ToLower() switch
            {
                "onetoone" => $@"        modelBuilder.Entity<{modelName}>()
            .HasOne<{relatedTo}>(s => s.{propertyName})
            .WithOne(e => e.{relatedPropertyName ?? propertyName})
            .HasForeignKey<{modelName}>(ad => ad.{propertyName}Id);",

                "onetomany" => $@"        modelBuilder.Entity<{modelName}>()
            .HasMany<{relatedTo}>(s => s.{propertyName})
            .WithOne(e => e.{relatedPropertyName ?? modelName})
            .HasForeignKey(s => s.{relatedPropertyName ?? modelName}Id);",

                "manytoone" => $@"        modelBuilder.Entity<{modelName}>()
            .HasOne<{relatedTo}>(s => s.{propertyName})
            .WithMany(e => e.{relatedPropertyName ?? $"{modelName}s"})
            .HasForeignKey(s => s.{propertyName}Id);",

                "manytomany" => $@"        modelBuilder.Entity<{modelName}>()
            .HasMany(m => m.{propertyName})
            .WithMany(r => r.{relatedPropertyName})
            .UsingEntity(j => j.ToTable(""{modelName}{relatedTo}s""));",

                _ => throw new ArgumentException($"Unsupported relationship type: {relationshipType}")
            };
        }

        private static int FindMethodEndPosition(string content, int methodStartIndex)
        {
            var methodStartBrace = content.IndexOf('{', methodStartIndex);
            var currentPos = methodStartBrace + 1;
            var openBraces = 1;

            while (openBraces > 0 && currentPos < content.Length)
            {
                if (content[currentPos] == '{')
                    openBraces++;
                else if (content[currentPos] == '}')
                    openBraces--;

                if (openBraces > 0)
                    currentPos++;
            }

            return currentPos;
        }

        private static string AddFieldToClass(
            string classCode,
            string fieldName,
            string fieldType,
            string collectionType = "",
            bool isRequired = false
        )
        {
            const string requiredAttribute = "    [Required]";
            var propertyCode = fieldType.Contains("ICollection")
                ? $"    public {fieldType} {fieldName} {{ get; set; }} = new List<{collectionType}>();"
                : $"    public {fieldType} {fieldName} {{ get; set; }}";

            var classCodeBuilder = new StringBuilder();
            
            if (isRequired)
            {
                classCodeBuilder.AppendLine(requiredAttribute);
            }
            classCodeBuilder.AppendLine(propertyCode);
       
            var newPropertyCode = classCodeBuilder.ToString();
            var insertPosition = classCode.LastIndexOf("}", StringComparison.Ordinal);
            var updatedCode = classCode.Insert(
                insertPosition,
                newPropertyCode + Environment.NewLine
            );

            return updatedCode;
        }

        private static Type? CreateModel(string modelName)
        {
            try
            {
                var assemblyName = new AssemblyName("DynamicAssembly");
                var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
                    assemblyName,
                    AssemblyBuilderAccess.Run
                );
                var moduleBuilder = assemblyBuilder.DefineDynamicModule("TempModule");

                var typeBuilder = moduleBuilder.DefineType(
                    modelName,
                    TypeAttributes.Public | TypeAttributes.Class
                );

                var ccControllerAttrConstructor = typeof(CCControllerAttribute).GetConstructor(
                    Type.EmptyTypes
                );
                var customAttributeBuilder = new CustomAttributeBuilder(
                    ccControllerAttrConstructor!,
                    []
                );
                typeBuilder.SetCustomAttribute(customAttributeBuilder);

                return typeBuilder.CreateType();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in CreateModel: {ex.Message}");
                return null;
            }
        }

        private static string GenerateClassCode(Type modelType)
        {
            var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
            var assemblyName = assembly.GetName().Name;
            var sb = new StringBuilder();

            sb.AppendLine("using System.ComponentModel.DataAnnotations;");
            sb.AppendLine("using System.ComponentModel.DataAnnotations.Schema;");
            sb.AppendLine("using Dappi.SourceGenerator.Attributes;");
            sb.AppendLine("using Dappi.HeadlessCms.Models;");
            sb.AppendLine();
            sb.AppendLine($"namespace {assemblyName}.Entities;");
            sb.AppendLine();
            sb.AppendLine("[CCController]");
            sb.AppendLine($"public class {modelType.Name}");
            sb.AppendLine("{");
            sb.AppendLine("    [Key]");
            sb.AppendLine("    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]");
            sb.AppendLine("    public Guid Id { get; set; }");
            sb.AppendLine();
            sb.AppendLine("}");

            return sb.ToString();
        }

        private static List<FieldsInfo> ExtractFieldsFromModel(string classCode)
        {
            var fieldList = new List<FieldsInfo>();
            var propertyPattern = new Regex(
                @"public\s+(required\s+)?([\w<>\[\]?]+)\s+(\w+)\s*\{\s*get;\s*set;\s*\}",
                RegexOptions.Multiline
            );

            var matches = propertyPattern.Matches(classCode);
            foreach (Match match in matches)
            {
                if (match.Groups.Count >= 4)
                {
                    var hasRequiredKeyword = !string.IsNullOrEmpty(match.Groups[1].Value);
                    var fieldType = match.Groups[2].Value;
                    var fieldName = match.Groups[3].Value;

                    var isNullable = fieldType.Contains("?");
                    var isRequired = hasRequiredKeyword || !isNullable;

                    fieldList.Add(
                        new FieldsInfo
                        {
                            FieldName = fieldName, FieldType = fieldType.Replace("?", ""), IsRequired = isRequired,
                        }
                    );
                }
            }

            return fieldList;
        }
    }
}