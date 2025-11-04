using System.Text.Json;
using System.Text.RegularExpressions;
using Dappi.HeadlessCms.Database;
using Dappi.HeadlessCms.Extensions;
using Dappi.HeadlessCms.Interfaces;
using Dappi.HeadlessCms.Models;
using Dappi.Core.Utils;
using Dappi.HeadlessCms.Core;
using Dappi.HeadlessCms.Core.Attributes;
using Dappi.HeadlessCms.Core.Schema;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Dappi.HeadlessCms.Controllers
{
    [ApiExplorerSettings(GroupName = "Toolkit")]
    [Route("api/models")]
    [ApiController]
    public class ModelsController : ControllerBase
    {
        private readonly DomainModelEditor _domainModelEditor;
        private readonly DbContextEditor _dbContextEditor;
        private readonly ICurrentDappiSessionProvider _currentSessionProvider;
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
            ICurrentDappiSessionProvider currentSessionProvider,
            DomainModelEditor domainModelEditor, DbContextEditor dbContextEditor)
        {
            _currentSessionProvider = currentSessionProvider;
            _domainModelEditor = domainModelEditor;
            _dbContextEditor = dbContextEditor;
            _dbContext = dappiDbContextAccessor.DbContext;

            if (!Directory.Exists(_entitiesFolderPath))
            {
                Directory.CreateDirectory(_entitiesFolderPath);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllModels()
        {
            try
            {
                var domainModelEntities = await _domainModelEditor.GetDomainModelEntityInfosAsync();
                var res = domainModelEntities.Select(x => x.Name).ToList();
                return Ok(res);
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
                _domainModelEditor.CreateEntityModel(request.ModelName, request.IsAuditableEntity);

                await AddContentTypeChangeAsync(
                    request.ModelName,
                    new Dictionary<string, string>() { { "Id", "Guid" } }
                );

                await _domainModelEditor.SaveAsync();

                var entitiesToBeRegistered = (await _domainModelEditor.GetDomainModelEntityInfosAsync()).ToList();
                foreach (var newModel in entitiesToBeRegistered)
                {
                    _dbContextEditor.AddDbSetToDbContext(newModel);
                }

                await _dbContextEditor.SaveAsync();

                return Ok(
                    new { Message = $"Model class '{request.ModelName}' created successfully." }
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

                var properties = _domainModelEditor.GetPropertiesContainingAttribute(modelName,
                    DappiRelationAttribute.ShortName).ToList();
                var relatedEntities = _domainModelEditor.GetRelatedEntities(properties);

                foreach (var relatedEntity in relatedEntities)
                {
                    _domainModelEditor.DeleteRelatedProperties(relatedEntity);
                }

                await _domainModelEditor.SaveAsync();
                System.IO.File.Delete(modelFilePath);

                _dbContextEditor.RemoveSetFromDbContext(new DomainModelEntityInfo()
                {
                    Name = modelName, Namespace = Directory.GetCurrentDirectory()
                });

                await _dbContextEditor.SaveAsync();

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

            if (request.FieldName == modelName)
            {
                return BadRequest($"Property name {request.FieldName} cannot be the same as the model name.");
            }

            try
            {
                var modelFilePath = Path.Combine(_entitiesFolderPath, $"{modelName}.cs");
                if (!System.IO.File.Exists(modelFilePath))
                {
                    return NotFound("Model class not found.");
                }

                var fieldDict = new Dictionary<string, string> { { request.FieldName, request.FieldType } };
                var relatedFieldDict = new Dictionary<string, string>();
                var existingCode = await System.IO.File.ReadAllTextAsync(modelFilePath);

                if (PropertyCheckUtils.PropertyNameExists(existingCode, request.FieldName))
                {
                    return BadRequest($"Property {request.FieldName} name already exists in {modelFilePath}.");
                }

                if (!string.IsNullOrEmpty(request.RelatedTo))
                {
                    switch (request.FieldType)
                    {
                        case Constants.Relations.OneToOne:
                            {
                                HandleOneToOneRelationship(request, modelName);
                                relatedFieldDict.Add(request.RelatedRelationName ?? modelName, request.FieldType);
                                break;
                            }
                        case Constants.Relations.OneToMany:
                            {
                                HandleOneToManyRelationship(request, modelName);
                                relatedFieldDict.Add(request.RelatedRelationName ?? modelName,
                                    Constants.Relations.ManyToOne);
                                break;
                            }
                        case Constants.Relations.ManyToOne:
                            {
                                HandleManyToOneRelationship(request, modelName);
                                relatedFieldDict.Add(request.RelatedRelationName ?? $"{modelName.Pluralize()}",
                                    Constants.Relations.OneToMany);
                                break;
                            }
                        case Constants.Relations.ManyToMany:
                            {
                                HandleManyToManyRelationship(request, modelName);
                                relatedFieldDict.Add(request.RelatedRelationName ?? $"{modelName.Pluralize()}",
                                    Constants.Relations.ManyToMany);
                                break;
                            }
                    }
                }
                else
                {
                    var property = new Property
                    {
                        DomainModel = modelName,
                        Name = request.FieldName,
                        Type = request.FieldType,
                        IsRequired = request.IsRequired,
                    };
                    _domainModelEditor.AddProperty(property);
                }

                await UpdateContentTypeChangeFieldsAsync(modelName, fieldDict);

                if (request.RelatedTo != null && relatedFieldDict.Count != 0)
                    await UpdateContentTypeChangeFieldsAsync(request.RelatedTo, relatedFieldDict);

                await _domainModelEditor.SaveAsync();
                await _dbContextEditor.SaveAsync();
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
                Console.WriteLine($"Error adding field: {ex.Message}");
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

        private async Task UpdateContentTypeChangeFieldsAsync(string modelName, Dictionary<string, string> newFields)
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

        private void HandleOneToOneRelationship(FieldRequest request, string modelName)
        {
            var foreignKeyRelatedName = $"{modelName}Id";
            Property property = new()
            {
                DomainModel = modelName,
                Name = request.FieldName,
                Type = request.RelatedTo!,
                IsRequired = request.IsRequired,
                RelationKind = DappiRelationKind.OneToOne,
                RelatedDomainModel = request.RelatedTo
            };
            _domainModelEditor.AddProperty(property);

            Property relatedModelProperty = new()
            {
                DomainModel = request.RelatedTo!,
                Name = request.RelatedRelationName ?? modelName,
                Type = modelName,
                IsRequired = request.IsRequired,
                RelationKind = DappiRelationKind.OneToOne,
                RelatedDomainModel = modelName
            };

            _domainModelEditor.AddProperty(relatedModelProperty);
            _domainModelEditor.AddProperty(relatedModelProperty with
            {
                Name = foreignKeyRelatedName, Type = nameof(Guid)
            });


            _dbContextEditor.UpdateOnModelCreating(modelName, request.RelatedTo!, Constants.Relations.OneToOne,
                request.FieldName,
                request.RelatedRelationName ?? modelName);
        }

        private void HandleOneToManyRelationship(FieldRequest request, string modelName)
        {
            var foreignKeyName = $"{modelName}Id";
            Property property = new()
            {
                DomainModel = modelName,
                Name = request.FieldName,
                Type = $"ICollection<{request.RelatedTo!}>",
                IsRequired = request.IsRequired,
                RelationKind = DappiRelationKind.OneToMany,
                RelatedDomainModel = request.RelatedTo
            };
            _domainModelEditor.AddProperty(property);

            Property relatedModelProperty = new()
            {
                DomainModel = request.RelatedTo!,
                Name = request.RelatedRelationName ?? modelName,
                Type = $"{modelName}",
                IsRequired = request.IsRequired,
                RelationKind = DappiRelationKind.ManyToOne,
                RelatedDomainModel = modelName
            };

            _domainModelEditor.AddProperty(relatedModelProperty);
            _domainModelEditor.AddProperty(relatedModelProperty with { Name = foreignKeyName, Type = nameof(Guid) });
            _dbContextEditor.UpdateOnModelCreating(modelName, request.RelatedTo!, Constants.Relations.OneToMany,
                request.FieldName,
                request.RelatedRelationName ?? modelName);
        }

        private void HandleManyToOneRelationship(FieldRequest request, string modelName)
        {
            var foreignKeyName = $"{request.RelatedTo}Id";
            Property property = new()
            {
                DomainModel = modelName,
                Name = request.FieldName,
                Type = request.RelatedTo!,
                IsRequired = request.IsRequired,
                RelationKind = DappiRelationKind.ManyToOne,
                RelatedDomainModel = request.RelatedTo
            };

            _domainModelEditor.AddProperty(property);
            _domainModelEditor.AddProperty(property with { Name = foreignKeyName, Type = nameof(Guid) });

            Property relatedModelProperty = new()
            {
                DomainModel = request.RelatedTo!,
                Name = request.RelatedRelationName ?? modelName.Pluralize(),
                Type = $"ICollection<{modelName}>",
                IsRequired = request.IsRequired,
                RelationKind = DappiRelationKind.OneToMany,
                RelatedDomainModel = modelName
            };

            _domainModelEditor.AddProperty(relatedModelProperty);

            _dbContextEditor.UpdateOnModelCreating(modelName, request.RelatedTo!, Constants.Relations.ManyToOne,
                request.FieldName,
                request.RelatedRelationName ?? $"{modelName.Pluralize()}");
        }

        private void HandleManyToManyRelationship(FieldRequest request, string modelName)
        {
            Property property = new()
            {
                DomainModel = modelName,
                Name = request.FieldName,
                Type = $"ICollection<{request.RelatedTo}>",
                IsRequired = request.IsRequired,
                RelationKind = DappiRelationKind.ManyToMany,
                RelatedDomainModel = request.RelatedTo
            };

            _domainModelEditor.AddProperty(property);

            Property relatedModelProperty = new()
            {
                DomainModel = request.RelatedTo!,
                Name = request.RelatedRelationName ?? modelName.Pluralize(),
                Type = $"ICollection<{modelName}>",
                IsRequired = request.IsRequired,
                RelationKind = DappiRelationKind.ManyToMany,
                RelatedDomainModel = modelName
            };
            _domainModelEditor.AddProperty(relatedModelProperty);

            _dbContextEditor.UpdateOnModelCreating(modelName, request.RelatedTo!, Constants.Relations.ManyToMany,
                request.FieldName,
                request.RelatedRelationName ?? $"{modelName.Pluralize()}");
        }

        private static List<FieldsInfo> ExtractFieldsFromModel(string classCode)
        {
            var auditableProps = new List<string> { "CreatedAtUtc", "UpdatedAtUtc", "CreatedBy", "UpdatedBy" };
            var fieldList = new List<FieldsInfo>();
            var propertyPattern = new Regex(
                @"public\s+(required\s+)?([\w<>\[\]?]+)\s+(\w+)\s*\{\s*get;\s*set;\s*\}",
                RegexOptions.Multiline
            );
            var isAuditableEntity = classCode.Contains("IAuditableEntity");
            var matches = propertyPattern.Matches(classCode);
            foreach (Match match in matches)
            {
                if (match.Groups.Count >= 4)
                {
                    if (isAuditableEntity && auditableProps.Contains(match.Groups[3].Value))
                    {
                        continue;
                    }

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