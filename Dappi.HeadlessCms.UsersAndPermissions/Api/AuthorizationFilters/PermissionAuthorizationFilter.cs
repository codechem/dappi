using System.Security.Claims;
using Dappi.HeadlessCms.UsersAndPermissions.Database;
using Dappi.HeadlessCms.UsersAndPermissions.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Dappi.HeadlessCms.UsersAndPermissions.Api.AuthorizationFilters;

public class PermissionAuthorizationFilter(
    AvailablePermissionsRepository permissionsRepository,
    IDbContextAccessor db,
    IMemoryCache cache
) : IAuthorizationFilter
{
    private readonly UsersAndPermissionsDbContext _db = db.DbContext;

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var controllerName = context.RouteData.Values["controller"] + "Controller";
        var actionName = context.RouteData.Values["action"]?.ToString();
        if (actionName is null)
            return;

        // If the controller doesn't have configured permissions by the UAP system, allow access by default
        if (!permissionsRepository.ControllerHasConfiguredPermissions(controllerName))
            return;

        var userRoles = context
            .HttpContext.User.Claims.Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();

        // User is external and doesn't have roles in the token, try to find them in the database based on the email claim
        if (userRoles.Count == 0)
        {
            var email = context
                .HttpContext.User.Claims.First(c => c.Type == ClaimTypes.Email)
                .Value;
            var role = _db
                .AppRoles.Include(appRole => appRole.Users)
                .FirstOrDefault(x => x.Users.Select(x => x.Email).Contains(email));

            if (role is null)
                context.Result = new ForbidResult();
            else
                userRoles.Add(role.Name);
        }

        var canAccess = userRoles.Any(userRole =>
        {
            var userId = context.HttpContext.User.Identity?.Name;
            var cacheKey = $"{userId}:perm:{userRole}:{controllerName}:{actionName}";

            return cache.GetOrCreate(
                cacheKey,
                entry =>
                {
                    entry.SlidingExpiration = TimeSpan.FromMinutes(15);

                    var permissionName = $"{controllerName}:{actionName}";

                    return _db
                        .AppRoles.Include(appRole => appRole.Permissions)
                        .Where(r => r.Name == userRole)
                        .SelectMany(r => r.Permissions)
                        .Any(p => p.Name == permissionName);
                }
            );
        });

        if (!canAccess)
            context.Result = new ForbidResult();
    }
}
