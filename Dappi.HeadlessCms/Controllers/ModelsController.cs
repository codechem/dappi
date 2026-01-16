using Dappi.Core.Utils;
using Dappi.HeadlessCms.Authentication;
using Dappi.HeadlessCms.Core;
using Dappi.HeadlessCms.Core.Attributes;
using Dappi.HeadlessCms.Core.Schema;
using Dappi.HeadlessCms.Enums;
using Dappi.HeadlessCms.Extensions;
using Dappi.HeadlessCms.Interfaces;
using Dappi.HeadlessCms.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dappi.HeadlessCms.Controllers;

[ApiController]
[ApiExplorerSettings(GroupName = "Toolkit")]
[Route("api/models")]
[Authorize(Policy = DappiAuthenticationSchemes.DappiAuthenticationScheme)]
public class ModelsController : ControllerBase
{
    private readonly DomainModelEditor _domainModelEditor;
    private readonly DbContextEditor _dbContextEditor;
    private readonly string _entitiesFolderPath;
    private readonly string _controllersFolderPath;
    private readonly IContentTypeChangesService _contentTypeChangesService;

    public ModelsController(DomainModelEditor domainModelEditor,
        DbContextEditor dbContextEditor,
        IContentTypeChangesService contentTypeChangesService)
    {
        _domainModelEditor = domainModelEditor;
        _dbContextEditor = dbContextEditor;
        _contentTypeChangesService = contentTypeChangesService;

        _entitiesFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "Entities");
        _controllersFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "Controllers");

        if (!Directory.Exists(_entitiesFolderPath))
        {
            Directory.CreateDirectory(_entitiesFolderPath);
        }
    }
    
    [HttpGet]
    public async Task<IActionResult> GetAllModels()
    {
        var domainModelEntities = await _domainModelEditor.GetDomainModelEntityInfosAsync();
        var res = domainModelEntities.Select(x => x.Name).ToList();
        return Ok(res);
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
            return BadRequest("Model name is invalid.");
        }

        var modelNames = DirectoryUtils.GetClassNamesFromDirectory(_entitiesFolderPath);
        if (modelNames.Contains(request.ModelName))
        {
            return BadRequest($"A model with the name '{request.ModelName}' already exists.");
        }


        _domainModelEditor.CreateEntityModel(request);

        await _contentTypeChangesService.AddContentTypeChangeAsync(
            request.ModelName,
            new Dictionary<string, string> { { "Id", "Guid" } },
            ContentTypeState.PendingPublish
        );

        await _domainModelEditor.SaveAsync();

        var entitiesToBeRegistered = (await _domainModelEditor.GetDomainModelEntityInfosAsync()).ToList();
        foreach (var newModel in entitiesToBeRegistered)
        {
            _dbContextEditor.AddDbSetToDbContext(newModel);
        }

        await _dbContextEditor.SaveAsync();

        return Ok(new { Message = $"Model class '{request.ModelName}' created successfully." });
    }

    [HttpDelete("{modelName}")]
    public async Task<IActionResult> DeleteModel(string modelName)
    {
        if (string.IsNullOrWhiteSpace(modelName))
        {
            return BadRequest("Model name must be provided.");
        }

        var modelFilePath = Path.Combine(_entitiesFolderPath, $"{modelName}.cs");


        if (!System.IO.File.Exists(modelFilePath))
        {
            return NotFound("Model file not found.");
        }

        var properties = _domainModelEditor
            .GetPropertiesContainingAttribute(modelName, DappiRelationAttribute.ShortName).ToList();
        var relatedEntities = _domainModelEditor.GetRelatedEntities(properties);

        _dbContextEditor.RemoveSetFromDbContext(new DomainModelEntityInfo
        {
            Name = modelName, Namespace = Directory.GetCurrentDirectory()
        });

        _dbContextEditor.DeleteRelations(modelName, relatedEntities);
        _dbContextEditor.UpdateUsings();
        await _dbContextEditor.SaveAsync();

        foreach (var relatedEntity in relatedEntities)
        {
            _domainModelEditor.DeleteRelatedProperties(relatedEntity, modelName);
        }

        await _domainModelEditor.SaveAsync();

        System.IO.File.Delete(modelFilePath);

        var controllerFilePath = Path.Combine(_controllersFolderPath, $"{modelName}Controller.cs");
        if (System.IO.File.Exists(controllerFilePath))
        {
            System.IO.File.Delete(controllerFilePath);
        }

        await _contentTypeChangesService.AddContentTypeChangeAsync(modelName, new Dictionary<string, string>(),
            ContentTypeState.PendingDelete);

        return Ok(new { Message = $"Model '{modelName}' deleted successfully.", FilePath = modelFilePath });
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
            return BadRequest($"Property name {request.FieldName} is invalid.");
        }

        if (request.FieldName == modelName)
        {
            return BadRequest($"Property name {request.FieldName} cannot be the same as the model name.");
        }


        var modelFilePath = Path.Combine(_entitiesFolderPath, $"{modelName}.cs");
        if (!System.IO.File.Exists(modelFilePath))
        {
            return NotFound("Model class not found.");
        }

        var fieldDict = new Dictionary<string, string> { { request.FieldName, request.FieldType } };
        var relatedFieldDict = new Dictionary<string, string>();

        var existingProperty = _domainModelEditor.GetProperty(modelName, request.FieldName);
        if (existingProperty != null)
        {
            return BadRequest($"Property {request.FieldName} name already exists in {modelFilePath}.");
        }

        if (!string.IsNullOrEmpty(request.RelatedTo))
        {
            switch (request.FieldType)
            {
                case Constants.Relations.OneToOne:
                    HandleOneToOneRelationship(request, modelName);
                    relatedFieldDict.Add(request.RelatedRelationName ?? modelName, request.FieldType);
                    break;

                case Constants.Relations.OneToMany:
                    HandleOneToManyRelationship(request, modelName);
                    relatedFieldDict.Add(request.RelatedRelationName ?? modelName, Constants.Relations.ManyToOne);
                    break;

                case Constants.Relations.ManyToOne:
                    HandleManyToOneRelationship(request, modelName);
                    relatedFieldDict.Add(request.RelatedRelationName ?? $"{modelName.Pluralize()}",
                        Constants.Relations.OneToMany);
                    break;

                case Constants.Relations.ManyToMany:
                    {
                        HandleManyToManyRelationship(request, modelName);
                        relatedFieldDict.Add(request.RelatedRelationName ?? $"{modelName.Pluralize()}",
                            Constants.Relations.ManyToMany);
                        break;
                    }
                default:
                    {
                        Property property = new()
                        {
                            DomainModel = modelName,
                            Name = request.FieldName,
                            Type = request.FieldType,
                            IsRequired = request.IsRequired,
                            Regex = request.Regex,
                            NoPastDates = request.NoPastDates,
                            MinLength = request.MinLength?.ToString(),
                            MaxLength = request.MaxLength?.ToString(),
                            MinValue = request.MinValue?.ToString(System.Globalization.CultureInfo.InvariantCulture),
                            MaxValue = request.MaxValue?.ToString(System.Globalization.CultureInfo.InvariantCulture)
                        };
                        _domainModelEditor.AddProperty(property);
                        _domainModelEditor.AddEnumNamespaceIfMissing(property.DomainModel);
                        await _domainModelEditor.SaveAsync();
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
                Regex = request.Regex,
                NoPastDates = request.NoPastDates,
                MinLength = request.MinLength?.ToString(),
                MaxLength = request.MaxLength?.ToString(),
                MinValue = request.MinValue?.ToString(System.Globalization.CultureInfo.InvariantCulture),
                MaxValue = request.MaxValue?.ToString(System.Globalization.CultureInfo.InvariantCulture)
            };
            _domainModelEditor.AddProperty(property);
        }

        await _contentTypeChangesService.UpdateContentTypeChangeFieldsAsync(modelName, fieldDict);

        if (request.RelatedTo != null && relatedFieldDict.Count != 0)
        {
            await _contentTypeChangesService.UpdateContentTypeChangeFieldsAsync(request.RelatedTo,
                relatedFieldDict);
        }

        await _domainModelEditor.SaveAsync();
        await _dbContextEditor.SaveAsync();

        return Ok(new
        {
            Message =
                $"Field '{request.FieldName}' of type '{request.FieldType}' added successfully to '{modelName}' model.",
            FilePath = modelFilePath
        });
    }

    [HttpPut("configure-actions/{modelName}")]
    public async Task<IActionResult> ConfigureActions([FromRoute] string modelName,
        [FromBody] ConfigureModelActionsRequest request)
    {
        var modelFilePath = Path.Combine(_entitiesFolderPath, $"{modelName}.cs");
        if (!System.IO.File.Exists(modelFilePath))
        {
            return NotFound($"Model '{modelName}' not found.");
        }

        _domainModelEditor.ConfigureActions(modelName, request.CrudActions.ToArray());
        Dictionary<string, string> newActions = new() { { "Actions", $"[{string.Join(", ", request.CrudActions)}]" } };
        await _contentTypeChangesService.AddContentTypeChangeAsync(modelName,
            newActions,
            ContentTypeState.PendingActionsChange);

        await _domainModelEditor.SaveAsync();

        return Ok(new { message = "Actions configured successfully." });
    }

    [HttpGet("fields/{modelName}")]
    public async Task<IActionResult> GetModelFields(string modelName)
    {
        var res = new ModelResponse
        {
            Fields = await _domainModelEditor.GetFieldsInfoAsync(modelName),
            AllowedActions = await _domainModelEditor.GetAllowedActionsAsync(modelName)
        };
        return Ok(res);
    }

    [HttpPatch("{modelName}/fields")]
    public async Task<IActionResult> UpdateField(string modelName, [FromBody] UpdateFieldRequest request)
    {
        if (string.IsNullOrEmpty(modelName))
        {
            return BadRequest("Model name must be provided.");
        }

        if (request.OldFieldName.Equals("Id", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("The 'Id' field cannot be edited.");
        }

        if (!request.NewFieldName.IsValidClassNameOrPropertyName())
        {
            return BadRequest($"Property name {request.NewFieldName} is invalid.");
        }

        var modelFilePath = Path.Combine(_entitiesFolderPath, $"{modelName}.cs");
        if (!System.IO.File.Exists(modelFilePath))
        {
            return NotFound($"Model '{modelName}' not found.");
        }

        var oldProperty = _domainModelEditor.GetProperty(modelName, request.OldFieldName);
        if (oldProperty == null)
        {
            return BadRequest($"Property {request.OldFieldName} does not exist in {modelName}.");
        }

        if (request.OldFieldName != request.NewFieldName)
        {
            var newProperty = _domainModelEditor.GetProperty(modelName, request.NewFieldName);
            if (newProperty != null)
            {
                return BadRequest($"Property {request.NewFieldName} already exists in {modelName}.");
            }
        }

        var propertyToUpdate = _domainModelEditor.GetProperty(modelName, request.OldFieldName);
        if (propertyToUpdate == null)
        {
            return NotFound($"Could not find property {request.OldFieldName} in model {modelName}.");
        }

        _domainModelEditor.UpdateProperty(modelName, request.OldFieldName, new Property
        {
            DomainModel = modelName,
            Name = request.NewFieldName,
            Type = propertyToUpdate.Type,
            IsRequired = request.IsRequired,
            Regex = request.Regex,
            NoPastDates = request.NoPastDates,
            RelationKind = propertyToUpdate.RelationKind,
            RelatedDomainModel = propertyToUpdate.RelatedDomainModel,
            MinLength = request.MinLength?.ToString(),
            MaxLength = request.MaxLength?.ToString(),
            MinValue = request.MinValue?.ToString(System.Globalization.CultureInfo.InvariantCulture),
            MaxValue = request.MaxValue?.ToString(System.Globalization.CultureInfo.InvariantCulture)
        });

        if (request.OldFieldName != request.NewFieldName)
        {
            _dbContextEditor.UpdatePropertyNameInOnModelCreating(modelName, request.OldFieldName, request.NewFieldName);
        }

        if (request.HasIndex && !propertyToUpdate.HasIndex)
        {
            _dbContextEditor.UpdateOnModelCreatingWithIndexedColumn(modelName, request.NewFieldName);
        }

        var fieldUpdateDict = new Dictionary<string, string>
        {
            { request.NewFieldName, $"{propertyToUpdate.Type}_UPDATED" }
        };
        
        await _contentTypeChangesService.UpdateContentTypeChangeFieldsAsync(modelName, fieldUpdateDict);

        await _domainModelEditor.SaveAsync();
        await _dbContextEditor.SaveAsync();

        return Ok(new
        {
            Message = $"Field '{request.OldFieldName}' updated successfully to '{request.NewFieldName}' in '{modelName}' model.",
            FilePath = modelFilePath
        });
    }

    [HttpDelete("{modelName}/fields/{fieldName}")]
    public async Task<IActionResult> DeleteField(string modelName, string fieldName)
    {
        if (string.IsNullOrEmpty(modelName))
        {
            return BadRequest("Model name must be provided.");
        }

        if (string.IsNullOrEmpty(fieldName))
        {
            return BadRequest("Field name must be provided.");
        }

        if (fieldName.Equals("Id", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("The 'Id' field cannot be deleted.");
        }

        var modelFilePath = Path.Combine(_entitiesFolderPath, $"{modelName}.cs");
        if (!System.IO.File.Exists(modelFilePath))
        {
            return NotFound("Model class not found.");
        }

        var property = _domainModelEditor.GetProperty(modelName, fieldName);
        if (property == null)
        {
            return NotFound($"Property {fieldName} does not exist in {modelName}.");
        }        

        await _domainModelEditor.DeleteProperty(modelName, fieldName);
        _dbContextEditor.RemovePropertyFromOnModelCreating(modelName, fieldName);

        var fieldDeleteDict = new Dictionary<string, string>
        {
            { fieldName, "DELETED" }
        };
        await _contentTypeChangesService.UpdateContentTypeChangeFieldsAsync(modelName, fieldDeleteDict);

        await _domainModelEditor.SaveAsync();
        await _dbContextEditor.SaveAsync();

        return Ok(new
        {
            Message = $"Field '{fieldName}' deleted successfully from '{modelName}' model.",
            FilePath = modelFilePath
        });
    }

    [HttpGet("hasRelatedProperties/{modelName}")]
    public IActionResult HasRelatedProperties(string modelName)
    {
        if (string.IsNullOrWhiteSpace(modelName))
        {
            return BadRequest("Model name must be provided.");
        }

        var modelFilePath = Path.Combine(_entitiesFolderPath, $"{modelName}.cs");
        if (!System.IO.File.Exists(modelFilePath))
        {
            return NotFound("Model class not found.");
        }

        var properties = _domainModelEditor
            .GetPropertiesContainingAttribute(modelName, DappiRelationAttribute.ShortName).ToList();
        var hasRelatedProperties = _domainModelEditor.GetRelatedEntities(properties).Count > 0;

        return Ok(new { hasRelatedProperties });
    }

    private void HandleOneToOneRelationship(FieldRequest request, string modelName)
    {
        var foreignKeyRelatedName = $"{modelName}Id";

        var property = new Property
        {
            DomainModel = modelName,
            Name = request.FieldName,
            Type = request.RelatedTo!,
            IsRequired = request.IsRequired,
            RelationKind = DappiRelationKind.OneToOne,
            RelatedDomainModel = request.RelatedTo
        };
        _domainModelEditor.AddProperty(property);

        var relatedModelProperty = new Property
        {
            DomainModel = request.RelatedTo!,
            Name = request.RelatedRelationName ?? modelName,
            Type = modelName,
            IsRequired = request.IsRequired,
            RelationKind = DappiRelationKind.OneToOne,
            RelatedDomainModel = modelName
        };
        _domainModelEditor.AddProperty(relatedModelProperty);
        _domainModelEditor.AddProperty(relatedModelProperty with { Name = foreignKeyRelatedName, Type = nameof(Guid) });

        _dbContextEditor.UpdateOnModelCreating(modelName, request.RelatedTo!, Constants.Relations.OneToOne,
            request.FieldName, request.RelatedRelationName ?? modelName);
    }

    private void HandleOneToManyRelationship(FieldRequest request, string modelName)
    {
        var foreignKeyName = $"{modelName}Id";

        var property = new Property
        {
            DomainModel = modelName,
            Name = request.FieldName,
            Type = $"ICollection<{request.RelatedTo!}>",
            IsRequired = request.IsRequired,
            RelationKind = DappiRelationKind.OneToMany,
            RelatedDomainModel = request.RelatedTo
        };
        _domainModelEditor.AddProperty(property);

        var relatedModelProperty = new Property
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
            request.FieldName, request.RelatedRelationName ?? modelName);
    }

    private void HandleManyToOneRelationship(FieldRequest request, string modelName)
    {
        var foreignKeyName = $"{request.RelatedTo}Id";

        var property = new Property
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

        var relatedModelProperty = new Property
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
            request.FieldName, request.RelatedRelationName ?? $"{modelName.Pluralize()}");
    }

    private void HandleManyToManyRelationship(FieldRequest request, string modelName)
    {
        var property = new Property
        {
            DomainModel = modelName,
            Name = request.FieldName,
            Type = $"ICollection<{request.RelatedTo}>",
            IsRequired = request.IsRequired,
            RelationKind = DappiRelationKind.ManyToMany,
            RelatedDomainModel = request.RelatedTo
        };
        _domainModelEditor.AddProperty(property);

        var relatedModelProperty = new Property
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
            request.FieldName, request.RelatedRelationName ?? $"{modelName.Pluralize()}");
    }
}