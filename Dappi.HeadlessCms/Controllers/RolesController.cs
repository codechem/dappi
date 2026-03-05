using Dappi.HeadlessCms.Authentication;
using Dappi.HeadlessCms.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Dappi.HeadlessCms.Controllers;

[ApiController]
[Route("api/roles")]
[Authorize(Policy = DappiAuthenticationSchemes.DappiAuthenticationScheme, Roles = "Admin")]
public class RolesController : ControllerBase
{
    private readonly RoleManager<DappiRole> _roleManager;
    private readonly UserManager<DappiUser> _userManager;

    public RolesController(RoleManager<DappiRole> roleManager, UserManager<DappiUser> userManager)
    {
        _roleManager = roleManager;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> GetRoles([FromQuery] string searchTerm = "")
    {
        var query = _roleManager.Roles.AsQueryable();

        searchTerm = searchTerm.Trim().ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(role => role.Name != null && role.Name.ToLower().Contains(searchTerm));
        }

        var totalCount = query.Count();
        var roles = query
            .OrderBy(role => role.Name)
            .ToList();

        var roleDtos = new List<RoleDto>();
        foreach (var role in roles)
        {
            var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name!);
            roleDtos.Add(new RoleDto
            {
                Id = role.Id,
                Name = role.Name ?? string.Empty,
                UserCount = usersInRole.Count
            });
        }

        var response = new
        {
            Total = totalCount,
            Data = roleDtos
        };

        return Ok(response);
    }

    [HttpPost]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            return BadRequest(new { message = "Role name is required" });
        }

        var roleName = dto.Name.Trim();

        if (await _roleManager.RoleExistsAsync(roleName))
        {
            return Conflict(new { message = $"Role '{roleName}' already exists" });
        }

        var role = new DappiRole { Name = roleName };
        var createResult = await _roleManager.CreateAsync(role);
        if (!createResult.Succeeded)
        {
            return BadRequest(createResult.Errors);
        }

        return Ok(new RoleDto
        {
            Id = role.Id,
            Name = role.Name ?? roleName
        });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteRole(string id)
    {
        var role = await _roleManager.FindByIdAsync(id);
        if (role == null)
        {
            return NotFound(new { message = $"Role with id '{id}' not found" });
        }

        var deleteResult = await _roleManager.DeleteAsync(role);
        if (!deleteResult.Succeeded)
        {
            return BadRequest(deleteResult.Errors);
        }

        return Ok(new { message = $"Role '{role.Name}' deleted successfully" });
    }
}
