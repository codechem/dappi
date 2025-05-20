using System.Data;
using System.Text.Json;
using CCApi.Extensions.DependencyInjection.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

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
        public async Task<IActionResult> GetContentTypeChanges()
        {
            try
            {
                var contentTypeChanges = new List<ContentTypeChangeDto>();

                var connection = _dbContext.Database.GetDbConnection();
                await connection.OpenAsync();

                var sql = @"
            SELECT 
                ""Id"", ""ModelName"", ""Fields"", ""ModifiedBy"", ""ModifiedAt"", ""IsPublished""
            FROM ""ContentTypeChanges""
            WHERE DATE(""ModifiedAt"") = CURRENT_DATE
            ORDER BY ""ModifiedAt"" DESC";

                using var command = connection.CreateCommand();
                command.CommandText = sql;

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    DateTimeOffset modifiedAt;

                    if (reader.GetFieldType(reader.GetOrdinal("ModifiedAt")) == typeof(DateTimeOffset))
                    {
                        modifiedAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("ModifiedAt"));
                    }
                    else
                    {
                        DateTime dateTime = reader.GetDateTime(reader.GetOrdinal("ModifiedAt"));
                        modifiedAt = new DateTimeOffset(dateTime, TimeSpan.Zero);
                    }

                    contentTypeChanges.Add(new ContentTypeChangeDto
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("Id")),
                        ModelName = reader.GetString(reader.GetOrdinal("ModelName")),
                        Fields = DeserializeFields(reader.GetString(reader.GetOrdinal("Fields"))),
                        ModifiedBy = reader.IsDBNull(reader.GetOrdinal("ModifiedBy"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("ModifiedBy")),
                        ModifiedAt = modifiedAt,
                        IsPublished = reader.GetBoolean(reader.GetOrdinal("IsPublished"))
                    });
                }

                return Ok(contentTypeChanges);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving ContentTypeChanges");
                return StatusCode(500, new { message = "Internal server error" });
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