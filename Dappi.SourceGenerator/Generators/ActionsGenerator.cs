using Dappi.Core.Attributes;
using Dappi.Core.Enums;
using Dappi.Core.Utils;
using Dappi.SourceGenerator.Models;
using static Dappi.SourceGenerator.Utilities.ClassPropertiesAnalyzer;

namespace Dappi.SourceGenerator.Generators
{
    public static class ActionsGenerator
    {
        public static string GenerateGetByIdAction(List<CrudActions> crudActions, SourceModel item, string includesCode)
        {
            if (!crudActions.Contains(CrudActions.GetOne))
            {
                return string.Empty;
            }

            return $$"""

                     [HttpGet("{id}")]
                     {{PropagateDappiAuthorizationTags(item.AuthorizeAttributes, AuthorizeMethods.Get)}}
                     public async Task<IActionResult> Get{{item.ClassName}}(Guid id, [FromQuery] string? fields = null)
                     {
                         try
                         {
                             if (id == Guid.Empty)
                                 return BadRequest();

                             var query = dbContext.{{item.ClassName.Pluralize()}}.AsNoTracking().AsQueryable();
                            
                             query = query{{includesCode}};

                             var result = await query
                                 .FirstOrDefaultAsync(p => p.Id == id);

                             if (result is null)
                                 return NotFound();

                             return Ok(shaper.ShapeObject(result,fields));
                         } 
                         catch(PropertyNotFoundException ex)
                         {
                             return BadRequest(new {message = ex.Message});
                         }
                     }
                         
                     """;
        }

        public static string GenerateGetAction(List<CrudActions> crudActions, SourceModel item, string includesCode)
        {
            if (!crudActions.Contains(CrudActions.Get))
            {
                return string.Empty;
            }

            return $$"""

                     [HttpGet]
                     {{PropagateDappiAuthorizationTags(item.AuthorizeAttributes, AuthorizeMethods.Get)}}
                     [CollectionFilter]
                     public async Task<IActionResult> Get{{item.ClassName.Pluralize()}}([FromQuery] {{item.ClassName}}Filter? filter, [FromQuery] string? fields = null)
                     {
                         try
                         {
                             var query = dbContext.{{item.ClassName.Pluralize()}}.AsNoTracking().AsQueryable();
                            
                             query = query{{includesCode}};

                             var filters = HttpContext.Items[CollectionFilter.FilterParamsKey] as List<Filter>;
                             if (filters is not null && filters.Count > 0)
                             {
                                 query = LinqExtensions.ApplyFiltering(query, filter);
                                 query = query.ApplyFilter(filters);
                             }

                             if (!string.IsNullOrEmpty(filter.SortBy))
                             {
                                 query = LinqExtensions.ApplySorting(query, filter.SortBy, filter.SortDirection);
                             }

                             var total = await query.CountAsync();
                             var data = await query
                                 .Skip(filter.Offset)
                                 .Take(filter.Limit)
                                 .ToListAsync();

                             var listDto = new ListResponseDTO<ExpandoObject>
                             {
                                 Data = data.Select(x => shaper.ShapeObject(x,fields)),
                                 Limit = filter.Limit,
                                 Offset = filter.Offset,
                                 Total = total
                             };

                             return Ok(listDto);
                         }
                         catch(PropertyNotFoundException ex)
                         {
                             return BadRequest(new {message = ex.Message});
                         }
                     }
                         
                     """;
        }

        public static string GenerateGetAllAction(List<CrudActions> crudActions, SourceModel item, string includesCode)
        {
            if (!crudActions.Contains(CrudActions.GetAll))
            {
                return string.Empty;
            }

            return $$"""

                         [HttpGet("get-all")]
                         {{PropagateDappiAuthorizationTags(item.AuthorizeAttributes, AuthorizeMethods.Get)}}
                         public async Task<IActionResult> GetAll{{item.ClassName.Pluralize()}}()
                         {
                             var query = dbContext.{{item.ClassName.Pluralize()}}.AsNoTracking();
                            
                             query = query{{includesCode}};
                             
                             return Ok(new {items = await query.ToListAsync()});
                         }
                         
                     """;
        }

        public static string GeneratePostAction(List<CrudActions> crudActions, SourceModel item,
            string collectionAddCode)
        {
            if (!crudActions.Contains(CrudActions.Create))
            {
                return string.Empty;
            }

            return $$"""

                         [HttpPost]
                         {{PropagateDappiAuthorizationTags(item.AuthorizeAttributes, AuthorizeMethods.Post)}}
                         public async Task<IActionResult> Create([FromBody] {{item.ClassName}} model)
                         {
                             if (model is null)
                                 return BadRequest();

                             var modelToSave = new {{item.ClassName}}();
                             modelToSave = model;

                     {{collectionAddCode}}

                             await dbContext.{{item.ClassName.Pluralize()}}.AddAsync(modelToSave);
                             await dbContext.SaveChangesAsync();

                             return CreatedAtAction(nameof(Create), new { id = modelToSave.Id }, modelToSave);
                         }

                     """;
        }

        public static string GeneratePatchAction(List<CrudActions> crudActions, SourceModel item, string includesCode)
        {
            if (!crudActions.Contains(CrudActions.Patch))
            {
                return string.Empty;
            }

            return $$"""
                        
                     [HttpPatch("{id}")]
                     {{PropagateDappiAuthorizationTags(item.AuthorizeAttributes, AuthorizeMethods.Patch)}}
                     public async Task<IActionResult> JsonPatch{{item.ClassName}}(Guid id, JsonDocument patchOperations)
                     {
                         if (patchOperations is null || id == Guid.Empty)
                             return BadRequest("Invalid data provided.");

                         var entity = await dbContext.{{item.ClassName.Pluralize()}}{{includesCode}}.FirstOrDefaultAsync(s => s.Id == id);
                         
                         if (entity is null)
                         {
                             return NotFound("{{item.ClassName}} with this id not found.");
                         }
                         
                         if (patchOperations.RootElement.ValueKind == JsonValueKind.Array)
                         {
                             foreach (var patchOperation in patchOperations.RootElement.EnumerateArray())
                             {
                                 var hasOperation = patchOperation.TryGetProperty(JsonPatchProperties.Operation, out var operation);
                                 var hasPath = patchOperation.TryGetProperty(JsonPatchProperties.Path, out var path);
                                 var hasValue = patchOperation.TryGetProperty(JsonPatchProperties.Value, out var value);
                                 
                                 if (!hasOperation)
                                     return BadRequest("Invalid data provided. The operation is a required property.");

                                 if (operation.ValueKind == JsonValueKind.String)
                                 {
                                     var propertyPathValue = path.GetString();
                                     TextInfo textInfo = CultureInfo.InvariantCulture.TextInfo;
                                     var propertyPath = propertyPathValue[0] == '/' ? propertyPathValue.Substring(1, propertyPathValue[propertyPathValue.Length - 1] == '/' ? propertyPathValue.Length - 2 : propertyPathValue.Length - 1) : propertyPathValue;
                                     propertyPath = textInfo.ToTitleCase(propertyPath);
                                     var (propertyEntity, property, propertyEntityInterfaces, isEnumerable, isCollection) = GetEntityProperty(entity, propertyPath);
                                     var propertyInterfaces = property?.PropertyType?.GetInterfaces();
                                     switch (operation.GetString())
                                     {
                                         case JsonPatchOperations.Add:
                                             if (!hasPath || !hasValue)
                                                 return BadRequest("Invalid data provided. Path and value are required properties for the add operation.");

                                             if (property.PropertyType.IsGenericType &&
                                                 (propertyInterfaces.Contains(typeof(ICollection)) || propertyInterfaces.Contains(typeof(IEnumerable))))
                                             {
                                                 dynamic propertyList = property.GetValue(propertyEntity);
                                                 dynamic deserializedValue = value.Deserialize(property.PropertyType.GetGenericArguments()[0]);
                                                 propertyList?.Add(deserializedValue);
                                                 property.SetValue(propertyEntity, propertyList);
                                             }
                                             else
                                             {
                                                 SetValueToProperty(propertyEntity, property, value);
                                             }
                                             break;
                                         case JsonPatchOperations.Replace:
                                             if (!hasPath || !hasValue)
                                                 return BadRequest("Invalid data provided. Path and value are required properties for the replace operation.");
                                             
                                             SetValueToProperty(propertyEntity, property, value);
                                             break;
                                         case JsonPatchOperations.Remove:
                                             if (!hasPath)
                                                 return BadRequest("Invalid data provided. The path is a required property for the remove operation.");

                                             if (propertyEntity.GetType().IsGenericType &&
                                                 (isCollection || isEnumerable))
                                             {
                                                 var itemIndex = propertyPath.Substring(propertyPath.LastIndexOf("/",
                                                         StringComparison.InvariantCultureIgnoreCase) + 1, 1);
                                                 var enumerableList = propertyEntity as IEnumerable<object>;
                                                 if (int.TryParse(itemIndex, out int index))
                                                 {
                                                     var arrayElement = enumerableList.ElementAt(index);
                                                     dbContext.Remove(arrayElement);
                                                 }
                                             }
                                             else
                                                 SetValueToProperty(propertyEntity, property, null);
                                             break;
                                         case JsonPatchOperations.Test:
                                             if (!hasPath || !hasValue)
                                                 return BadRequest("Invalid data provided. Path and value are required properties for the test operation.");

                                             var result = property.GetValue(propertyEntity).Equals(value.Deserialize(property.PropertyType));
                                             if (result)                            
                                                 return Ok(result);
                                             return BadRequest(result);
                                         case JsonPatchOperations.Copy:
                                             var hasSource = patchOperation.TryGetProperty(JsonPatchProperties.From, out var from);
                                             if (!hasPath || !hasSource)
                                                 return BadRequest("Invalid data provided. Path and from are required properties for the copy operation.");

                                             if (path.ValueKind == JsonValueKind.String && from.ValueKind == JsonValueKind.String)
                                             {
                                                 var sourcePath = from.GetString();
                                                 var destinationPath = propertyPath;
                                                 if (string.IsNullOrEmpty(sourcePath) && string.IsNullOrEmpty(destinationPath))
                                                 {
                                                     return BadRequest("Invalid data provided.");
                                                 }
                                                 var sourcePropertyPath = sourcePath[0] == '/' ? sourcePath.Substring(1, sourcePath[sourcePath.Length - 1] == '/' ? sourcePath.Length - 2 : sourcePath.Length - 1) : sourcePath;
                                                 sourcePropertyPath = textInfo.ToTitleCase(sourcePropertyPath);
                                                 var (sourceEntity, sourceProperty, sourceEntityInterfaces, isSourceEnumerable, isSourceCollection) = GetEntityProperty(entity, sourcePropertyPath);
                                                 property.SetValue(propertyEntity, sourceProperty.GetValue(sourceEntity));
                                             }
                                             break;
                                     }
                                 }
                             }
                         }
                         else
                         {
                             return BadRequest("Data is not in valid format.");
                         }
                         await dbContext.SaveChangesAsync();
                         return Ok(entity);
                     }
                     """;
        }

        public static string GeneratePostActionForMediaInfo(List<CrudActions> crudActions, SourceModel item)
        {
            var containsMediaInfoProperty = item.PropertiesInfos.Any(p => p.PropertyType.Name.Contains("MediaInfo"));
            if (!crudActions.Contains(CrudActions.Create) || !containsMediaInfoProperty)
            {
                return string.Empty;
            }

            return $$"""

                         [HttpPost("upload-file/{id}")]
                         {{PropagateDappiAuthorizationTags(item.AuthorizeAttributes, AuthorizeMethods.Post)}}
                         public async Task<IActionResult> UploadFile(Guid id, IFormFile file, [FromForm] string fieldName)
                         {
                             if (string.IsNullOrEmpty(fieldName))
                                 return BadRequest("Field name is required.");

                             try
                             {
                                 var entity = await dbContext.{{item.ClassName.Pluralize()}}.FindAsync(id);

                                 if (entity == null)
                                     return NotFound($"{{item.ClassName}} with ID {id} not found.");

                                 var property = typeof({{item.ClassName}}).GetProperty(fieldName);
                                 if (property == null)
                                     return BadRequest($"Property {fieldName} does not exist.");

                                 if (property.PropertyType != typeof(MediaInfo))
                                     return BadRequest($"Property {fieldName} must be a MediaInfo type to store media information.");

                                 var mediaInfo = await uploadService.UploadMediaAsync(id, file);
                                 property.SetValue(entity, mediaInfo);

                                 await dbContext.Set<MediaInfo>().AddAsync(mediaInfo);
                                 await dbContext.SaveChangesAsync();

                                 dbContext.Entry(entity).State = EntityState.Modified;
                                 await dbContext.SaveChangesAsync();

                                 return Ok(mediaInfo);
                             }
                             catch (Exception ex)
                             {
                                 return BadRequest(new { message = ex.Message });
                             }
                         }
                         
                     """;
        }

        public static string GeneratePutAction(List<CrudActions> crudActions, SourceModel item, string includesCode,
            string collectionUpdateCode, string mediaInfoUpdateCode)
        {
            if (!crudActions.Contains(CrudActions.Update))
            {
                return string.Empty;
            }

            return $$"""

                         [HttpPut("{id}")]
                         {{PropagateDappiAuthorizationTags(item.AuthorizeAttributes, AuthorizeMethods.Put)}}
                         public async Task<IActionResult> Update(Guid id, [FromBody] {{item.ClassName}} model)
                         {
                             if (model == null || id == Guid.Empty)
                                 return BadRequest("Invalid data provided.");

                             var existingModel = await dbContext.{{item.ClassName.Pluralize()}}{{includesCode}}
                                 .FirstOrDefaultAsync(p => p.Id == id);
                                 
                             if (existingModel == null)
                                 return NotFound($"{{item.ClassName}} with ID {id} not found.");

                             model.Id = id;

                     {{collectionUpdateCode}}
                     {{mediaInfoUpdateCode}}

                             dbContext.Entry(existingModel).CurrentValues.SetValues(model);

                             await dbContext.SaveChangesAsync();
                             return Ok(existingModel);
                         }
                         
                     """;
        }

        public static string GenerateDeleteAction(List<CrudActions> crudActions, SourceModel item, string includeCode,
            string removeCode)
        {
            if (!crudActions.Contains(CrudActions.Delete))
            {
                return string.Empty;
            }

            return $$"""

                         [HttpDelete("{id}")]
                         {{PropagateDappiAuthorizationTags(item.AuthorizeAttributes, AuthorizeMethods.Delete)}}
                         public async Task<IActionResult> Delete(Guid id)
                         {
                             {{includeCode}}

                             if (model is null)
                                 return NotFound();

                             dbContext.{{item.ClassName.Pluralize()}}.Remove(model);
                             {{removeCode}}

                             await dbContext.SaveChangesAsync();

                             return Ok();
                         }
                         
                     """;
        }
    }
}