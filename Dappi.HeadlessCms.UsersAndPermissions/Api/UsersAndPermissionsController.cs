using Dappi.Core.Exceptions;
using Dappi.HeadlessCms.UsersAndPermissions.Core;
using Dappi.HeadlessCms.UsersAndPermissions.Core.Exceptions;
using Dappi.HeadlessCms.UsersAndPermissions.Database;
using Dappi.HeadlessCms.UsersAndPermissions.Interfaces;
using Dappi.HeadlessCms.UsersAndPermissions.Models;
using Dappi.HeadlessCms.UsersAndPermissions.Services;
using Microsoft.AspNetCore.Authorization;
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
    TokenService<TUser> tokenService,
    IEmailService? service,
    IInvitationService invitationService) : ControllerBase
    where TUser : AppUser, new()
{
    private readonly UsersAndPermissionsDbContext _usersAndPermissionsDb =
        usersAndPermissionsDb.DbContext;


    [HttpPost]
    public async Task<IActionResult> InviteUser([FromBody] InviteUserDto dto)
    {
        var existingUser = await userManager.Users.FirstOrDefaultAsync(user =>
            (user.UserName ?? string.Empty).ToLower() == dto.Username.ToLower() ||
            (user.Email ?? string.Empty).ToLower() == dto.Email.ToLower());

        if (existingUser is not null)
        {
            if (string.Equals(existingUser.UserName, dto.Username, StringComparison.OrdinalIgnoreCase))
            {
                throw new UserAlreadyExistsException("An account already exists with conflicting username");
            }

            if (string.Equals(existingUser.Email, dto.Email, StringComparison.OrdinalIgnoreCase))
            {
                throw new UserAlreadyExistsException("An account already exists with conflicting email");
            }
        }

        InvitationPreparationResult? invitation = null;
        var passwordToUse = dto.Password;
        var isInvitationFlow = service is not null && string.IsNullOrWhiteSpace(passwordToUse);

        if (isInvitationFlow)
        {
            var requestBaseUrl = $"{Request.Scheme}://{Request.Host}";
            invitation = await invitationService.PrepareInvitationAsync(dto, requestBaseUrl);
            passwordToUse = invitation.GeneratedPassword;
        }
        else if (string.IsNullOrWhiteSpace(passwordToUse))
        {
            return BadRequest(new
            {
                message = "Password is required to create a user.",
            });
        }

        var rolesToAssign = dto.Roles.Count > 0
            ? dto.Roles
            : new List<string> { UsersAndPermissionsConstants.DefaultRoles.Authenticated };

        var normalizedRequestedRoles = rolesToAssign
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .Select(role => role.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (normalizedRequestedRoles.Count == 0)
        {
            normalizedRequestedRoles = [UsersAndPermissionsConstants.DefaultRoles.Authenticated];
        }

        var availableRoles = await _usersAndPermissionsDb.AppRoles.ToListAsync();
        var assignedRole = availableRoles.FirstOrDefault(role =>
            normalizedRequestedRoles.Any(requested =>
                string.Equals(requested, role.Name, StringComparison.OrdinalIgnoreCase)
            )
        );
        if (assignedRole is null)
        {
            return BadRequest(new
            {
                message = "At least one valid role is required for invitation.",
            });
        }

        var user = new TUser
        {
            UserName = dto.Username,
            Email = dto.Email,
            EmailConfirmed = !isInvitationFlow,
            RoleId = assignedRole.Id,
        };

        var result = await userManager.CreateAsync(user, passwordToUse!);

        if (!result.Succeeded)
        {
            return BadRequest(new
            {
                message = result.Errors.FirstOrDefault()?.Description ?? "Failed to create invited user.",
            });
        }

        if (!isInvitationFlow)
        {
            return Ok(new
            {
                message = "User created successfully.",
            });
        }

        if (invitation is null)
        {
            return BadRequest(new
            {
                message = "Failed to prepare invitation.",
            });
        }

        var messageId = await service!.SendEmailAsync(
            [dto.Email],
            invitation.EmailHtmlBody,
            invitation.EmailTextBody,
            invitation.EmailSubject
        );

        return Ok(new
        {
            message = "Invitation sent successfully.",
            emailSent = true,
            messageId,
            invitationLink = invitation.AcceptUrl,
            frontendInvitationLink = invitation.FrontendAcceptUrl,
            fallbackApiAcceptLink = invitation.AcceptUrl
        });
    }

    [AllowAnonymous]
    [HttpGet("accept-invitation")]
    public async Task<IActionResult> AcceptInvitation([FromQuery] AcceptInvitationQueryDto dto)
    {
        if (!invitationService.TryGetInvitationPayload(dto.Token, out var invitation, out var tokenError))
        {
            return BadRequest(new { message = tokenError });
        }

        if (invitation.ExpiresAtUtc < DateTime.UtcNow)
        {
            return BadRequest(new { message = "Invitation token has expired." });
        }

        var existingUsers = await userManager.Users
            .Where(user =>
                (user.UserName ?? string.Empty).ToLower() == invitation.Username.ToLower() ||
                (user.Email ?? string.Empty).ToLower() == invitation.Email.ToLower())
            .Take(2)
            .ToListAsync();

        if (existingUsers.Count > 1)
        {
            return BadRequest(new
            {
                message = "An invitation-related account already exists with conflicting data.",
            });
        }

        var existingUser = existingUsers.FirstOrDefault();

        if (existingUser is null)
        {
            return BadRequest(new { message = "Invitation-related user account was not found." });
        }

        var requestBaseUrl = $"{Request.Scheme}://{Request.Host}";
        var completeInvitationUrl = invitationService.BuildCompleteInvitationUrl(requestBaseUrl, dto.Token);

        return Redirect(completeInvitationUrl);
    }

    [AllowAnonymous]
    [HttpPost("complete-invitation")]
    public async Task<IActionResult> CompleteInvitation([FromBody] CompleteInvitationDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Token))
        {
            return BadRequest(new { message = "Invitation token is required." });
        }

        if (string.IsNullOrWhiteSpace(dto.OldPassword) || string.IsNullOrWhiteSpace(dto.NewPassword))
        {
            return BadRequest(new { message = "Both old and new passwords are required." });
        }

        if (!invitationService.TryGetInvitationPayload(dto.Token, out var invitation, out var tokenError))
        {
            return BadRequest(new { message = tokenError });
        }

        if (invitation.ExpiresAtUtc < DateTime.UtcNow)
        {
            return BadRequest(new { message = "Invitation token has expired." });
        }

        var user = await userManager.FindByNameAsync(invitation.Username);
        if (user is null || !string.Equals(user.Email, invitation.Email, StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "Invitation is not accepted yet." });
        }

        var passwordMatches = await userManager.CheckPasswordAsync(user, dto.OldPassword);
        if (!passwordMatches)
        {
            return BadRequest(new { message = "Old password is incorrect." });
        }

        var changePasswordResult = await userManager.ChangePasswordAsync(
            user,
            dto.OldPassword,
            dto.NewPassword
        );

        if (!changePasswordResult.Succeeded)
        {
            return BadRequest(new
            {
                message = changePasswordResult.Errors.FirstOrDefault()?.Description ?? "Failed to change password.",
            });
        }

        if (!user.EmailConfirmed)
        {
            user.EmailConfirmed = true;
        }

        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return BadRequest(new
            {
                message = updateResult.Errors.FirstOrDefault()?.Description ?? "Password changed, but failed to finalize invitation.",
            });
        }

        return Ok(new { message = "Password changed successfully. User verified." });
    }



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
            .FirstOrDefaultAsync(
                r => r.Name.ToLower() == roleName.Trim().ToLower(),
                cancellationToken
            );

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

    [HttpGet("roles")]
    public async Task<IActionResult> GetAllRoles()
    {
        var roles = await _usersAndPermissionsDb.AppRoles
            .Select(r => new
            {
                r.Name,
                r.Id,
                r.IsDefaultForAuthenticatedUser
            })
            .ToListAsync();
        return Ok(roles);
    }
    
    [HttpGet("users")]
    public async Task<IActionResult> GetAllUsers(CancellationToken cancellationToken)
    {
        var users = await userManager
            .Users.Include(u => u.Role)
            .Select(u => new PluginUserDto
            {
                Id = u.Id,
                UserName = u.UserName ?? string.Empty,
                Email = u.Email ?? string.Empty,
                EmailConfirmed = u.EmailConfirmed,
                RoleId = u.RoleId,
                RoleName = u.Role != null ? u.Role.Name : string.Empty,
            })
            .ToListAsync(cancellationToken);

        return Ok(users);
    }

    [HttpDelete("users/{id:int}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return NotFound(new { message = "User not found" });
        }

        var result = await userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return Ok(new { message = "User deleted successfully" });
    }
}
