using Dappi.Core.Utils;
using Dappi.HeadlessCms.Authentication;
using Dappi.HeadlessCms.Core;
using Dappi.HeadlessCms.Database;
using Dappi.HeadlessCms.Enums;
using Dappi.HeadlessCms.Interfaces;
using Dappi.HeadlessCms.Models.Mapping;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Dappi.HeadlessCms.Controllers
{
    [ApiController]
    [Route("api/content-type-changes")]
    [Authorize(Policy = DappiAuthenticationSchemes.DappiAuthenticationScheme)]
    public class ContentTypeChangesController : ControllerBase
    {
        private readonly DappiDbContext _dbContext;
        private readonly ILogger<ContentTypeChangesController> _logger;
        private readonly DomainModelEditor _domainModelEditor;
        private readonly IContentTypeChangesService _contentTypeChangesService;

        public ContentTypeChangesController(
            IDbContextAccessor dappiDbContextAccessor,
            ILogger<ContentTypeChangesController> logger,
            ICurrentDappiSessionProvider currentSessionProvider,
            DomainModelEditor domainModelEditor, IContentTypeChangesService contentTypeChangesService)
        {
            _dbContext = dappiDbContextAccessor.DbContext;
            _logger = logger;
            _domainModelEditor = domainModelEditor;
            _contentTypeChangesService = contentTypeChangesService;
        }

        [HttpGet]
        public async Task<IActionResult> GetContentTypeChanges()
        {
            var dto = _dbContext.ContentTypeChanges
                .AsNoTracking()
                .Where(ctc => ctc.ModifiedAt >= DateTimeOffset.UtcNow.AddDays(-1))
                .OrderByDescending(ctc => ctc.ModifiedAt)
                .ToDtos();

            return Ok(await dto.ToListAsync());
        }

        [HttpGet("published-models")]
        public async Task<IActionResult> GetPublishedModels()
        {
            var models = (await _domainModelEditor.GetDomainModelEntityInfosAsync())
                .Select(x => x.Name).ToList();
            var existingTables = _dbContext.Model.GetEntityTypes().Select(x => x.GetTableName()).ToList();

            var publishedOnlyModels = await _dbContext.ContentTypeChanges
                .Where(ctc =>
                    ctc.State == ContentTypeState.Published &&
                    models.Contains(ctc.ModelName))
                .Select(x => x.ModelName)
                .Distinct()
                .ToListAsync();

            var filteredPublishedModels =
                publishedOnlyModels.Where(x => existingTables.Any(e => e == x.Pluralize())).ToList();
            return Ok(filteredPublishedModels);
        }

        [HttpGet("draft-models")]
        public async Task<IActionResult> GetDraftModels()
        {
            var draftModels = _contentTypeChangesService.GetDraftsAsync()
                .Select(x => x.ModelName)
                .Distinct();

            return Ok(await draftModels.ToListAsync());
        }
    }
}