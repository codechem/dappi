using Dappi.HeadlessCms.Core;
using Microsoft.AspNetCore.Mvc;

namespace Dappi.HeadlessCms.Controllers;

[ApiExplorerSettings(GroupName = "Toolkit")]
[Route("api/update-db-context")]
[ApiController]
public class UpdateAppDbContextControllerrr(DbContextEditor dbContextEditor, DomainModelEditor domainModelEditor)
    : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> UpdateDbContext()
    {
        try
        {
            var entitiesToBeRegistered = (await domainModelEditor.GetDomainModelEntityInfos()).ToList();
            foreach (var newModel in entitiesToBeRegistered)
            {
                 dbContextEditor.AddDbSetToDbContext(newModel);
            }

            await dbContextEditor.SaveAsync();

            return Ok(new
            {
                Message = "AppDbContext updated with new DbSets.",
                NewModels = entitiesToBeRegistered
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }
}