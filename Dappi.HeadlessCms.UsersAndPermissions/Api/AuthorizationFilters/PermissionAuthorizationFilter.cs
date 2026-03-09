using System.Security.Claims;
using Dappi.HeadlessCms.UsersAndPermissions.Core;
using Dappi.HeadlessCms.UsersAndPermissions.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Dappi.HeadlessCms.UsersAndPermissions.Api.AuthorizationFilters;

public class PermissionAuthorizationFilter(IDbContextAccessor db, IMemoryCache cache)
    : IAuthorizationFilter
{
    private readonly UsersAndPermissionsDbContext _db = db.DbContext;

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var controllerName = context.RouteData.Values["controller"];
        var actionName = context.RouteData.Values["action"]?.ToString();

        if (controllerName is null || actionName is null)
            return;

        var userRoles = context
            .HttpContext.User.Claims.Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();

        var canAccess = userRoles.Any(role =>
        {
            var userId = context.HttpContext.User.Identity?.Name;
            var cacheKey = $"{userId}:perm:{role}:{controllerName}:{actionName}";

            return cache.GetOrCreate(
                cacheKey,
                entry =>
                {
                    entry.SlidingExpiration = TimeSpan.FromMinutes(15);

                    var permissionName = $"{controllerName}Controller:{actionName}";

                    return _db
                        .AppRoles.Include(appRole => appRole.Permissions)
                        .Where(r => r.Name == role)
                        .SelectMany(r => r.Permissions)
                        .Any(p => p.Name == permissionName);
                }
            );
        });

        if (!canAccess)
            context.Result = new ForbidResult();
    }
}
