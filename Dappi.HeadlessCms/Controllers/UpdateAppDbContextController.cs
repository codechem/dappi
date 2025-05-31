using Dappi.HeadlessCms.Core;
using Microsoft.AspNetCore.Mvc;

namespace Dappi.HeadlessCms.Controllers;

[ApiExplorerSettings(GroupName = "Toolkit")]
[Route("api/update-db-context")]
[ApiController]
public class UpdateAppDbContextController : ControllerBase
{
    private readonly string _domainModelPath = Path.Combine(Directory.GetCurrentDirectory(), "Entities");

    private readonly string _appDbContextFilePath =
        Path.Combine(Directory.GetCurrentDirectory(), "Data", "AppDbContext.cs");
   
    [HttpGet]
    public async Task<IActionResult> UpdateDbContext()
    {
        try
        {
            using var dbContextEditor = new DbContextEditor(_appDbContextFilePath);
            using var domainModelEditor = new DomainModelEditor(_domainModelPath);
            var allDomainModelEntities = await domainModelEditor.GetDomainModelEntitiesAsync();
            var entitiesToBeRegistered = (await dbContextEditor.GetUnregisteredDomainModelEntitiesAsync(allDomainModelEntities)).ToList();

            foreach (var newModel in entitiesToBeRegistered)
            {
                await dbContextEditor.AddDbSetAsync(newModel);
            }

            if (dbContextEditor.HasChanges)
            {
                await dbContextEditor.SaveAsync();
            }

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