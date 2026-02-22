using System.Text.Json.Serialization;
using Dappi.Core.Models;
using Dappi.HeadlessCms.UsersAndPermissions.Core;
using Dappi.HeadlessCms.UsersAndPermissions.Database;
using Dappi.HeadlessCms.UsersAndPermissions.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Dappi.HeadlessCms.UsersAndPermissions
{
    public static class Extensions
    {
        public static IServiceCollection AddUsersAndPermissionsSystem<TDbContext>(
            this IServiceCollection services,
            IReadOnlyDictionary<string, IReadOnlyList<MethodRouteEntry>> controllerRoutes,
            IConfiguration configuration
        )
            where TDbContext : UsersAndPermissionsDbContext
        {
            services.AddDbContext<TDbContext>(
                (_, options) =>
                {
                    var dataSourceBuilder = new NpgsqlDataSourceBuilder(
                        configuration.GetValue<string>("Dappi:PostgresConnection")
                    );

                    options.UseNpgsql(dataSourceBuilder.Build());
                }
            );
            services.AddScoped<IDbContextAccessor, DbContextAccessor<TDbContext>>();
            services.AddSingleton(new AvailablePermissionsRepository(controllerRoutes));

            services
                .AddControllers()
                .AddApplicationPart(
                    typeof(Dappi.HeadlessCms.UsersAndPermissions.Controllers.RolesController).Assembly
                )
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.PropertyNamingPolicy = null;
                    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                });
            return services;
        }

        public static async Task<IApplicationBuilder> UseUsersAndPermissionsSystem<
            TDbContext,
            TUser
        >(this WebApplication app, AppRoleAndPermissionsBuilder<TUser> appRoleAndPermissionsBuilder)
            where TDbContext : UsersAndPermissionsDbContext
            where TUser : class, IAppUser
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TDbContext>();
            var allPermissions = scope
                .ServiceProvider.GetRequiredService<AvailablePermissionsRepository>()
                .GetAllPermissions();

            await db.Database.MigrateAsync();

            db.AppPermissions.RemoveRange(db.AppPermissions);
            await db.SaveChangesAsync();

            await db.AppPermissions.AddRangeAsync(allPermissions);
            await db.SaveChangesAsync();

            var permissionsByName = await db.AppPermissions.ToDictionaryAsync(p => p.Name);

            var builtRoles = appRoleAndPermissionsBuilder.Build();

            var existingRoles = await db.AppRoles.Include(r => r.Permissions).ToListAsync();

            var existingRolesByName = existingRoles.ToDictionary(r => r.Name);

            var builtRoleNames = builtRoles.Select(r => r.Name).ToHashSet();

            var orphanedRoles = await db
                .AppRoles.Where(r => !builtRoleNames.Contains(r.Name))
                .Where(r => !db.Set<TUser>().Any(u => u.RoleId == r.Id))
                .ToListAsync();

            db.AppRoles.RemoveRange(orphanedRoles);

            foreach (var builtRole in builtRoles)
            {
                var resolvedPermissions = builtRole
                    .Permissions.Where(p => permissionsByName.ContainsKey(p.Name))
                    .Select(p => permissionsByName[p.Name])
                    .ToList();

                if (existingRolesByName.TryGetValue(builtRole.Name, out var existingRole))
                {
                    existingRole.ClearPermissions();
                    foreach (var perm in resolvedPermissions)
                        existingRole.AddPermission(perm);
                }
                else
                {
                    var newRole = builtRole.Name switch
                    {
                        UsersAndPermissionsConstants.DefaultRoles.Public =>
                            AppRole.CreateDefaultPublicUserRole(resolvedPermissions),

                        UsersAndPermissionsConstants.DefaultRoles.Authenticated =>
                            AppRole.CreateDefaultAuthenticatedUserRole(resolvedPermissions),

                        _ => new AppRole(builtRole.Name, resolvedPermissions),
                    };

                    await db.AppRoles.AddAsync(newRole);
                }
            }

            await db.SaveChangesAsync();
            app.MapControllers();

            return app;
        }
    }
}
