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
                         public async Task<IActionResult> Get{{item.ClassName}}(Guid id)
                         {
                             if (id == Guid.Empty)
                                 return BadRequest();

                             var query = dbContext.{{item.ClassName.Pluralize()}}.AsNoTracking().AsQueryable();
                            
                             query = query{{includesCode}};

                             var result = await query
                                 .FirstOrDefaultAsync(p => p.Id == id);

                             if (result is null)
                                 return NotFound();

                             return Ok(result);
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
                         public async Task<IActionResult> Get{{item.ClassName.Pluralize()}}([FromQuery] {{item.ClassName}}Filter? filter)
                         {
                             var query = dbContext.{{item.ClassName.Pluralize()}}.AsNoTracking().AsQueryable();
                            
                             query = query{{includesCode}};

                             if (filter != null)
                             {
                                 query = LinqExtensions.ApplyFiltering(query, filter);
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

                             var listDto = new ListResponseDTO<{{item.ClassName}}>
                             {
                                 Data = data,
                                 Limit = filter.Limit,
                                 Offset = filter.Offset,
                                 Total = total
                             };

                             return Ok(listDto);
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