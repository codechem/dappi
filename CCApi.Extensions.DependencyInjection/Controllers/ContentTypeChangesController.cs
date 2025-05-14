using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using CCApi.Extensions.DependencyInjection.Models;
using CCApi.Extensions.DependencyInjection.Interfaces;
using CCApi.Extensions.DependencyInjection.Services;

namespace CCApi.Extensions.DependencyInjection.Controllers
{
    [ApiController]
    [Route("api/content-type-changes")]
    public class ContentTypeChangesController : ControllerBase
    {
        private readonly DbContext _dbContext;
        private readonly ILogger<ContentTypeChangesController> _logger;

        public ContentTypeChangesController(
            IDbContextAccessor dbContextAccessor,
            ILogger<ContentTypeChangesController> logger)
        {
            _dbContext = dbContextAccessor.DbContext;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetContentTypeChanges(
            [FromQuery] int offset = 0,
            [FromQuery] int limit = 10,
            [FromQuery] string searchTerm = "",
            [FromQuery] bool? isPublished = null)
        {
            try
            {
                string whereClause = "";
                List<object> parameters = new List<object>();

                searchTerm = searchTerm?.Trim().ToLower() ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    whereClause += @" WHERE (LOWER(""ModelName"") LIKE @p0 OR LOWER(""ModifiedBy"") LIKE @p0)";
                    parameters.Add($"%{searchTerm}%");
                }

                if (isPublished.HasValue)
                {
                    if (string.IsNullOrEmpty(whereClause))
                        whereClause = " WHERE";
                    else
                        whereClause += " AND";

                    whereClause += @" ""IsPublished"" = @p" + parameters.Count;
                    parameters.Add(isPublished.Value);
                }

                string countSql = $@"SELECT COUNT(*) FROM ""ContentTypeChanges""{whereClause}";
                var totalCount = await _dbContext.Database.ExecuteSqlRawAsync(countSql, parameters.ToArray());

                var connection = _dbContext.Database.GetDbConnection();
                if (connection.State != ConnectionState.Open)
                    await connection.OpenAsync();

                int total = 0;

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = countSql;

                    for (int i = 0; i < parameters.Count; i++)
                    {
                        var parameter = command.CreateParameter();
                        parameter.ParameterName = $"p{i}";
                        parameter.Value = parameters[i];
                        command.Parameters.Add(parameter);
                    }

                    var result = await command.ExecuteScalarAsync();
                    if (result != null && result != DBNull.Value)
                    {
                        total = Convert.ToInt32(result);
                    }
                }

                string dataSql = $@"
                    SELECT 
                        ""Id"", ""ModelName"", ""Fields"", ""ModifiedBy"", ""ModifiedAt"", ""IsPublished""
                    FROM ""ContentTypeChanges""{whereClause}
                    ORDER BY ""ModifiedAt"" DESC
                    LIMIT @p{parameters.Count} OFFSET @p{parameters.Count + 1}";

                var contentTypeChanges = new List<ContentTypeChangeDto>();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = dataSql;

                    for (int i = 0; i < parameters.Count; i++)
                    {
                        var parameter = command.CreateParameter();
                        parameter.ParameterName = $"p{i}";
                        parameter.Value = parameters[i];
                        command.Parameters.Add(parameter);
                    }

                    var limitParam = command.CreateParameter();
                    limitParam.ParameterName = $"p{parameters.Count}";
                    limitParam.Value = limit;
                    command.Parameters.Add(limitParam);

                    var offsetParam = command.CreateParameter();
                    offsetParam.ParameterName = $"p{parameters.Count + 1}";
                    offsetParam.Value = offset;
                    command.Parameters.Add(offsetParam);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            try
                            {
                                var dto = new ContentTypeChangeDto
                                {
                                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                    ModelName = reader.GetString(reader.GetOrdinal("ModelName")),
                                    Fields = DeserializeFields(reader.GetString(reader.GetOrdinal("Fields"))),
                                    ModifiedBy = reader.IsDBNull(reader.GetOrdinal("ModifiedBy")) ? null : reader.GetString(reader.GetOrdinal("ModifiedBy")),
                                    ModifiedAt = reader.GetDateTime(reader.GetOrdinal("ModifiedAt")),
                                    IsPublished = reader.GetBoolean(reader.GetOrdinal("IsPublished"))
                                };
                                contentTypeChanges.Add(dto);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error reading ContentTypeChange record");
                            }
                        }
                    }
                }

                var response = new PagedResponseDto<ContentTypeChangeDto>
                {
                    Total = total,
                    Offset = offset,
                    Limit = limit,
                    Data = contentTypeChanges
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving content type changes");
                return StatusCode(500, new { message = "An error occurred while retrieving content type changes" });
            }
        }

        [HttpGet("published-models")]
        public async Task<IActionResult> GetPublishedModels()
        {
            try
            {
                string sql = @"
                    SELECT DISTINCT ""ModelName""
                    FROM ""ContentTypeChanges""
                    WHERE ""ModelName"" NOT IN (
                        SELECT DISTINCT ""ModelName""
                        FROM ""ContentTypeChanges""
                        WHERE ""IsPublished"" = false
                    )
                    ORDER BY ""ModelName"" ASC";

                var publishedModels = new List<string>();

                var connection = _dbContext.Database.GetDbConnection();
                if (connection.State != ConnectionState.Open)
                    await connection.OpenAsync();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sql;

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            try
                            {
                                string modelName = reader.GetString(reader.GetOrdinal("ModelName"));
                                publishedModels.Add(modelName);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error reading ModelName");
                            }
                        }
                    }
                }

                return Ok(publishedModels);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving published models");
                return StatusCode(500, new { message = "An error occurred while retrieving published models" });
            }
        }

        [HttpGet("draft-models")]
        public async Task<IActionResult> GetDraftModels()
        {
            try
            {
                string sql = @"
                    SELECT DISTINCT ""ModelName""
                    FROM ""ContentTypeChanges""
                    WHERE ""ModelName"" IN (
                        SELECT DISTINCT ""ModelName""
                        FROM ""ContentTypeChanges""
                        WHERE ""IsPublished"" = false
                    )
                    ORDER BY ""ModelName"" ASC";

                var draftModels = new List<string>();

                var connection = _dbContext.Database.GetDbConnection();
                if (connection.State != ConnectionState.Open)
                    await connection.OpenAsync();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sql;

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            try
                            {
                                string modelName = reader.GetString(reader.GetOrdinal("ModelName"));
                                draftModels.Add(modelName);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error reading ModelName");
                            }
                        }
                    }
                }

                return Ok(draftModels);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving draft models");
                return StatusCode(500, new { message = "An error occurred while retrieving draft models" });
            }
        }

        private Dictionary<string, string> DeserializeFields(string fieldsJson)
        {
            if (string.IsNullOrEmpty(fieldsJson))
            {
                return new Dictionary<string, string>();
            }

            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                return JsonSerializer.Deserialize<Dictionary<string, string>>(fieldsJson, options);
            }
            catch (JsonException)
            {
                _logger.LogWarning("Failed to deserialize fields JSON: {FieldsJson}", fieldsJson);
                return new Dictionary<string, string>();
            }
        }
    }
}