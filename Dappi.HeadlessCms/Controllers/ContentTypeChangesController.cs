using Dappi.HeadlessCms.Database;
using Dappi.HeadlessCms.Enums;
using Dappi.HeadlessCms.Interfaces;
using Dappi.HeadlessCms.Models.Mapping;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Dappi.HeadlessCms.Controllers
{
    [ApiController]
    [Route("api/content-type-changes")]
    public class ContentTypeChangesController : ControllerBase
    {
        private readonly DappiDbContext _dbContext;
        private readonly ILogger<ContentTypeChangesController> _logger;

        public ContentTypeChangesController(
            IDbContextAccessor dappiDbContextAccessor,
            ILogger<ContentTypeChangesController> logger,
            ICurrentDappiSessionProvider currentSessionProvider)
        {
            _dbContext = dappiDbContextAccessor.DbContext;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetContentTypeChanges()
        {
            try
            {
                var dto = _dbContext.ContentTypeChanges
                    .AsNoTracking()
                    .Where(ctc => ctc.ModifiedAt >= DateTimeOffset.UtcNow.AddDays(-1))
                    .OrderByDescending(ctc => ctc.ModifiedAt)
                    .ToDtos();

                return Ok(await dto.ToListAsync());
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
                var publishedOnlyModels = _dbContext.ContentTypeChanges
                    .Where(ctc => ctc.State == ContentTypeState.Published)
                    .Select(x => x.ModelName)
                    .Distinct();
                
                return Ok(await publishedOnlyModels.ToListAsync());
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
                var draftModels = _dbContext.ContentTypeChanges
                    .AsNoTracking()
                    .Where(ctc =>
                        ctc.State == ContentTypeState.PendingPublish || ctc.State == ContentTypeState.PendingDelete)
                    .Select(x => x.ModelName)
                    .Distinct();

                return Ok(await draftModels.ToListAsync());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving draft models");
                return StatusCode(500, new { message = "An error occurred while retrieving draft models" });
            }
        }
    }
}