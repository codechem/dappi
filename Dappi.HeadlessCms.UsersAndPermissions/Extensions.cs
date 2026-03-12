using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dappi.Core.Models;
using Dappi.HeadlessCms.UsersAndPermissions.Api;
using Dappi.HeadlessCms.UsersAndPermissions.Api.AuthorizationFilters;
using Dappi.HeadlessCms.UsersAndPermissions.Api.Configuration;
using Dappi.HeadlessCms.UsersAndPermissions.Api.Middleware;
using Dappi.HeadlessCms.UsersAndPermissions.Core;
using Dappi.HeadlessCms.UsersAndPermissions.Database;
using Dappi.HeadlessCms.UsersAndPermissions.Jwt;
using Dappi.HeadlessCms.UsersAndPermissions.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Npgsql;

namespace Dappi.HeadlessCms.UsersAndPermissions;

public static class Extensions
{
    public static IServiceCollection AddUsersAndPermissionsSystem<TDbContext, TUser>(
        this IServiceCollection services,
        IReadOnlyDictionary<string, IReadOnlyList<MethodRouteEntry>> controllerRoutes,
        IConfiguration configuration
    )
        where TDbContext : UsersAndPermissionsDbContext
        where TUser : AppUser, new()
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
        services.AddMemoryCache();
        services
            .AddIdentityCore<TUser>()
            .AddRoles<AppRole>()
            .AddEntityFrameworkStores<TDbContext>()
            .AddDefaultTokenProviders()
            .AddSignInManager();
        var systemJwtProvider = new SystemJwtValidationProvider<TUser>(configuration);
        services.AddSingleton(systemJwtProvider.SchemaAndIssuerProvider);

        services
            .AddAuthentication()
            .AddJwtBearer(
                systemJwtProvider.SchemaAndIssuerProvider.Schema,
                options =>
                {
                    options.TokenValidationParameters =
                        systemJwtProvider.BuildValidationParameters();

                    options.Events = systemJwtProvider.BuildEvents();
                }
            );
        services.AddScoped<TokenService<TUser>>();

        services.AddScoped<PermissionAuthorizationFilter>();
        services.AddScoped<ExternalUserSyncContext<TUser>>();

        services
            .AddControllers(opts =>
            {
                opts.Filters.AddService<PermissionAuthorizationFilter>();
                opts.Conventions.Add(new GenericControllerRouteConvention());
            })
            .AddApplicationPart(typeof(UsersAndPermissionsController<>).Assembly)
            .ConfigureApplicationPartManager(apm =>
                apm.FeatureProviders.Add(new GenericControllerFeatureProvider<TUser>())
            )
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = null;
                options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            });

        return services;
    }

    public static IServiceCollection AddExternalJwtProvider<TProvider, TUser>(
        this IServiceCollection services
    )
        where TProvider : JwtValidationProvider<TUser>, new()
        where TUser : AppUser, new()
    {
        var provider = new TProvider();

        services
            .AddAuthentication()
            .AddJwtBearer(
                provider.SchemaAndIssuerProvider.Schema,
                options =>
                {
                    options.TokenValidationParameters = provider.BuildValidationParameters();
                    options.Events = new JwtBearerEvents
                    {
                        OnTokenValidated = async ctx =>
                        {
                            var syncContext = ctx.HttpContext.RequestServices.GetRequiredService<
                                ExternalUserSyncContext<TUser>
                            >();
                            await provider.OnUserAuthenticatedAsync(ctx.Principal!, syncContext);
                        },
                    };
                }
            );

        services.AddSingleton(provider.SchemaAndIssuerProvider);
        return services;
    }

    /// <summary>
    /// Discovers all <see cref="RoleConfiguration{TUser}"/> subclasses in the supplied
    /// <paramref name="assemblies"/> (defaults to the calling assembly when none are given),
    /// builds the permissions from them and bootstraps the Users &amp; Permissions system.
    /// </summary>
    public static Task<IApplicationBuilder> UseUsersAndPermissionsSystem<TDbContext, TUser>(
        this WebApplication app,
        params Assembly[] assemblies
    )
        where TDbContext : UsersAndPermissionsDbContext
        where TUser : AppUser
    {
        var builder = new AppRoleAndPermissionsBuilder<TUser>();
        ApplyConfigurations(builder, assemblies);
        return app.SetupUsersAndPermissionsInStorage<TDbContext, TUser>(builder);
    }

    private static async Task<IApplicationBuilder> SetupUsersAndPermissionsInStorage<
        TDbContext,
        TUser
    >(this WebApplication app, AppRoleAndPermissionsBuilder<TUser> appRoleAndPermissionsBuilder)
        where TDbContext : UsersAndPermissionsDbContext
        where TUser : AppUser
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
        app.UseAuthentication();
        app.UseMiddleware<PublicRoleAssignmentMiddleware>();
        app.UseAuthorization();
        app.MapControllers();
        return app;
    }

    private static void ApplyConfigurations<TUser>(
        AppRoleAndPermissionsBuilder<TUser> builder,
        params Assembly[] assemblies
    )
        where TUser : AppUser
    {
        var configType = typeof(RoleConfiguration<TUser>);

        var configurations = assemblies
            .SelectMany(a =>
            {
                try
                {
                    return a.GetTypes();
                }
                catch
                {
                    return Array.Empty<Type>();
                }
            })
            .Where(t => !t.IsAbstract && configType.IsAssignableFrom(t))
            .Select(t => (RoleConfiguration<TUser>)Activator.CreateInstance(t)!)
            .ToList();

        foreach (var config in configurations)
        {
            var controllerStage = ((IRoleConfigurationBuilder<TUser>)builder).ForRole(
                config.RoleName
            );
            config.Configure(controllerStage);
        }
    }
}
