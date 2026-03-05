using System.Security.Claims;
using Dappi.HeadlessCms.UsersAndPermissions.Core;
using Microsoft.AspNetCore.Http;

namespace Dappi.HeadlessCms.UsersAndPermissions.Api.Middleware;

public class PublicRoleAssignmentMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            var publicClaims = new List<Claim>
            {
                new(ClaimTypes.Role, UsersAndPermissionsConstants.DefaultRoles.Public),
            };

            var publicIdentity = new ClaimsIdentity(publicClaims, "Public");
            context.User = new ClaimsPrincipal(publicIdentity);
        }

        await next(context);
    }
}
