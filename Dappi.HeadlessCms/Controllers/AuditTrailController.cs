using Dappi.HeadlessCms.Database;
using Dappi.HeadlessCms.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Dappi.HeadlessCms.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
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