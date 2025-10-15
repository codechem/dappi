using Dappi.HeadlessCms.Database;
using Dappi.HeadlessCms.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Dappi.HeadlessCms.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuditTrailController(IDbContextAccessor dappiDbContextAccessor) : ControllerBase
{
    private readonly DappiDbContext _dbContext = dappiDbContextAccessor.DbContext;

    [HttpGet("{entityId}")]
    public async Task<IActionResult> GetAuditTrail(string entityId)
    {
        var auditTrails = await _dbContext.AuditTrails
            .Where(a => a.EntityId == entityId)
            .OrderByDescending(a => a.DateUtc)
            .ToListAsync();

        return Ok(auditTrails);
    }
}