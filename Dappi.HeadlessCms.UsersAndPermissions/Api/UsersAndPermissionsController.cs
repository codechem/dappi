using Dappi.HeadlessCms.UsersAndPermissions.Core;
using Dappi.HeadlessCms.UsersAndPermissions.Core.Exceptions;
using Dappi.HeadlessCms.UsersAndPermissions.Database;
using Dappi.HeadlessCms.UsersAndPermissions.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Dappi.HeadlessCms.UsersAndPermissions.Api;

[ApiController]
[Route("api/[controller]")]
public class UsersAndPermissionsController<TUser>(
    IDbContextAccessor usersAndPermissionsDb,
    AvailablePermissionsRepository availablePermissionsRepository,
    UserManager<TUser> userManager,
    TokenService<TUser> tokenService
) : ControllerBase
    where TUser : AppUser, new()
{
    private readonly UsersAndPermissionsDbContext _usersAndPermissionsDb =
        usersAndPermissionsDb.DbContext;

    [HttpPost(UsersAndPermissionsConstants.AuthenticationRoutes.Register)]
    public async Task<IActionResult> Register([FromBody] RegisterUserDto userDto)
    {
        var defaultRole = await _usersAndPermissionsDb.AppRoles.FirstOrDefaultAsync(r =>
            r.IsDefaultForAuthenticatedUser
        );

        if (defaultRole is null)
        {
            throw new DefaultRoleNotSetException();
        }

        var user = new TUser
        {
            Email = userDto.Email,
            UserName = userDto.Email,
            RoleId = defaultRole.Id,
        };
        var result = await userManager.CreateAsync(user, userDto.Password);

        return !result.Succeeded ? throw new AuthenticationFailedException() : Ok(user);
    }

    [HttpPost(UsersAndPermissionsConstants.AuthenticationRoutes.Login)]
    public async Task<IActionResult> Login([FromBody] RegisterUserDto userDto)
    {
#pragma warning disable CA1862
        var user = await userManager
            .Users.Include(x => x.Role)
            .FirstOrDefaultAsync(x =>
                !(x.Email == null || x.Email.ToLower() != userDto.Email.ToLower())
            );
#pragma warning restore CA1862
        if (user == null || !await userManager.CheckPasswordAsync(user, userDto.Password))
        {
            throw new AuthenticationFailedException();
        }

        var authResult = await tokenService.GenerateTokens(user);
        return Ok(authResult);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenDto dto)
    {
        var result = await tokenService.RefreshAsync(dto.RefreshToken);

        return result is null ? throw new AuthenticationFailedException() : Ok(result);
    }

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
                list = [];
                result[controllerName] = list;
            }

            list.Add(
                new RolePermissionDto
                {
                    PermissionName = methodName,
                    Description = perm.Description,
                    Selected = assignedPermissionNames.Contains(perm.Name),
                }
            );
        }

        return Ok(result);
    }
}
