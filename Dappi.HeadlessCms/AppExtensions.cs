using Dappi.HeadlessCms.Database;
using Dappi.HeadlessCms.Enums;
using Dappi.HeadlessCms.Middleware;
using Dappi.HeadlessCms.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace Dappi.HeadlessCms;

public static class AppExtensions
{
    public static async Task<IApplicationBuilder> UseDappi<TDbContext>(
        this WebApplication app,
        Action<SwaggerUIOptions>? configureSwagger = null)
        where TDbContext : DappiDbContext
    {
        app.UseMiddleware<ExceptionHandlingMiddleware>();

        if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Test"))
        {
            app.UseSwagger();
            app.UseSwaggerUI(configureSwagger ?? (c =>
            {
                c.SwaggerEndpoint("/swagger/Toolkit/swagger.json", "Toolkit API v1");
                c.SwaggerEndpoint("/swagger/Default/swagger.json", "Default API v1");
                c.RoutePrefix = "swagger";
            }));
        }

        app.Use(async (context, next) =>
        {
            await next();

            if (!context.Request.Path.Value!.StartsWith("/api/") &&
                !Path.HasExtension(context.Request.Path) ||
                context.Response.StatusCode == 404)
            {
                if (!context.Response.HasStarted)
                {
                    context.Request.Path = "/index.html";
                    await next();
                }
            }
        });

        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.MapControllers();

        using var scope = app.Services.CreateScope();
        await MigrateIfNoModelsAreInDraftStateAsync<TDbContext>(scope.ServiceProvider);
        await app.Services.SeedRolesAndUsersAsync<DappiUser, DappiRole>();

        return app;
    }

    private static async Task MigrateIfNoModelsAreInDraftStateAsync<TDbContext>(
        IServiceProvider serviceProvider)
        where TDbContext : DappiDbContext
    {
        var logger = serviceProvider.GetRequiredService<ILogger<TDbContext>>();
        var dbContext = serviceProvider.GetRequiredService<TDbContext>();

        try
        {
            await dbContext.Database.MigrateAsync();
            await PublishContentTypeChangesAsync<TDbContext>(serviceProvider);
        }
        catch (InvalidOperationException ex)
            when (ex.Message.Contains("PendingModelChangesWarning"))
        {
            logger.LogWarning(
                "Unable to migrate schema changes due to pending model changes. Most probably you have models in draft state. {Message}",
                ex.Message);
        }
    }

    private static async Task SeedRolesAndUsersAsync<TUser, TRole>(
        this IServiceProvider serviceProvider)
        where TUser : IdentityUser, new()
        where TRole : IdentityRole, new()
    {
        using var scope = serviceProvider.CreateScope();

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<TRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<TUser>>();

        foreach (var roleName in Constants.UserRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var role = new TRole();
                typeof(TRole).GetProperty("Name")?.SetValue(role, roleName);

                await roleManager.CreateAsync(role);
            }
        }

        const string adminEmail = "admin@gmail.com";
        const string adminUsername = "admin";
        const string adminPassword = "Dappi@123";

        var adminUser = await userManager.FindByEmailAsync(adminEmail);

        if (adminUser == null)
        {
            var user = new TUser();
            typeof(TUser).GetProperty("UserName")?.SetValue(user, adminUsername);
            typeof(TUser).GetProperty("Email")?.SetValue(user, adminEmail);
            typeof(TUser).GetProperty("EmailConfirmed")?.SetValue(user, true);

            var result = await userManager.CreateAsync(user, adminPassword);

            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, "Admin");
            }
        }
    }

    private static async Task PublishContentTypeChangesAsync<TDbContext>(
        IServiceProvider serviceProvider)
        where TDbContext : DappiDbContext
    {
        try
        {
            var dbContext = serviceProvider.GetRequiredService<TDbContext>();

            await dbContext.ContentTypeChanges
                .Where(ctc =>
                    ctc.State == ContentTypeState.PendingPublish ||
                    ctc.State == ContentTypeState.PendingDelete ||
                    ctc.State == ContentTypeState.PendingActionsChange)
                .ExecuteUpdateAsync(setters =>
                    setters.SetProperty(
                        e => e.State,
                        e => e.State == ContentTypeState.PendingPublish
                            ? ContentTypeState.Published
                            : e.State == ContentTypeState.PendingDelete
                                ? ContentTypeState.Deleted
                                : e.State == ContentTypeState.PendingActionsChange
                                    ? ContentTypeState.ActionsChanged
                                    : e.State));
        }
        catch (Exception ex)
        {
            var logger = serviceProvider.GetRequiredService<ILogger<TDbContext>>();
            logger.LogError(
                "Error publishing content-type changes: {PublishContentChangesError}",
                ex);
            throw;
        }
    }
}
