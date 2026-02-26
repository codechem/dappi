using Dappi.HeadlessCms.UsersAndPermissions.Database;
using Dappi.HeadlessCms.UsersAndPermissions.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Dappi.HeadlessCms.UsersAndPermissions.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersAndPermissionsController(
    IDbContextAccessor usersAndPermissionsDb,
    AvailablePermissionsRepository availablePermissionsRepository
) : ControllerBase
{
    private readonly UsersAndPermissionsDbContext _usersAndPermissionsDb =
        usersAndPermissionsDb.DbContext;

    [HttpGet]
    public async Task<ActionResult<Dictionary<string, List<RolePermissionDto>>>> GetRolePermissions(
        string roleName,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrWhiteSpace(roleName))
            return BadRequest("Role name is required.");

        var role = await _usersAndPermissionsDb
            .AppRoles.Include(r => r.Permissions)
            .FirstOrDefaultAsync(r => r.Name == roleName, cancellationToken);

        if (role is null)
            return NotFound($"Role '{roleName}' not found.");

        var assignedPermissionNames = role
            .Permissions.Select(p => p.Name)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var availablePermissions = availablePermissionsRepository.GetAllPermissions();

        var result = new Dictionary<string, List<RolePermissionDto>>(
            StringComparer.OrdinalIgnoreCase
        );

        foreach (var perm in availablePermissions)
        {
            if (string.IsNullOrWhiteSpace(perm.Name))
                continue;

            var parts = perm.Name.Split(':', 2);
            if (parts.Length < 2)
                continue;

            var controllerName = parts[0];
            var methodName = parts[1];

            if (!result.TryGetValue(controllerName, out var list))
            {
                list = new List<RolePermissionDto>();
                result[controllerName] = list;
            }

            list.Add(
                new RolePermissionDto
                {
                    PermissionName = methodName,
                    Description = perm.Description ?? string.Empty,
                    Selected = assignedPermissionNames.Contains(perm.Name),
                }
            );
        }

        return Ok(result);
    }
}
