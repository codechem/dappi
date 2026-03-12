using Dappi.HeadlessCms.UsersAndPermissions.Core;
using Dappi.HeadlessCms.UsersAndPermissions.Database;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Dappi.HeadlessCms.UsersAndPermissions.Services;

/// <summary>
/// Passed into <see cref="JwtValidationProvider{TUser}.OnUserAuthenticatedAsync"/> so
/// the provider can query and create local users without depending directly on
/// the full Identity infrastructure.
/// </summary>
public sealed class ExternalUserSyncContext<TUser>(
    UserManager<TUser> userManager,
    IDbContextAccessor dbContextAccessor
)
    where TUser : AppUser, new()
{
    /// <summary>Access to ASP.NET Core Identity for user management.</summary>
    public UserManager<TUser> UserManager { get; } = userManager;

    /// <summary>Direct access to the Dappi users-and-permissions DB context.</summary>
    public UsersAndPermissionsDbContext DbContext { get; } = dbContextAccessor.DbContext;

    /// <summary>
    /// Convenience: finds a user by e-mail, or creates and persists a new one
    /// (with no password — external auth only) if none exists.
    /// Returns the user that was found or created.
    /// </summary>
    public async Task<TUser> FindOrCreateByEmailAsync(string email)
    {
        var existing = await UserManager.FindByEmailAsync(email);
        if (existing is not null)
            return existing;

        var defaultRole = await DbContext.AppRoles.FirstOrDefaultAsync(r =>
            r.IsDefaultForAuthenticatedUser
        );

        var newUser = new TUser
        {
            Email = email,
            UserName = email,
            RoleId = defaultRole?.Id ?? 0,
        };

        var result = await UserManager.CreateAsync(newUser);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException(
                $"Could not auto-provision external user '{email}': {errors}"
            );
        }

        return (await UserManager.FindByEmailAsync(email))!;
    }
}
