using System.Diagnostics;
using CCApi.Extensions.DependencyInjection;
using CCApi.Extensions.DependencyInjection.Constants;
using CCApi.Extensions.DependencyInjection.Database;
using CCApi.Extensions.DependencyInjection.Utils;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace Microsoft.Extensions.DependencyInjection;

public static class AppExtensions
{
    public static async Task<IApplicationBuilder> UseDappi<TDbContext>(this WebApplication app,
        Action<SwaggerUIOptions>? configureSwagger = null)
       where TDbContext : DappiDbContext
    {
        var scope = app.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<TDbContext>>();

        await HandlePortCleanupAsync(app);

        if (app.Environment.IsDevelopment())
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

            if (!context.Request.Path.Value.StartsWith("/api/") &&
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
        app.MapControllers();
        app.UseStaticFiles();

        var db = scope.ServiceProvider.GetRequiredService<TDbContext>();
        try
        {
            await db.Database.MigrateAsync();

            await app.Services.SeedRolesAndUsersAsync<DappiUser, DappiRole>();

            await PublishContentTypeChangesAsync<TDbContext>(scope.ServiceProvider);
        }
        // we can't migrate the schema changes, but we need to continue and just not publish un-published content-type changes.
        catch (InvalidOperationException e) when (e.Message.Contains("PendingModelChangesWarning"))
        {
            logger.LogWarning(
                "Unable to migrate schema changes due to pending model changes. Most probably you have models in draft state. {Message}",
                e.Message);
        }

        return app;
    }

    private static async Task HandlePortCleanupAsync(WebApplication app)
    {
        var args = Environment.GetCommandLineArgs();
        var isRestart = args.Contains("--restart");

        if (isRestart)
        {
            Console.WriteLine("Restart detected - skipping port cleanup");
            return;
        }

        var urls = app.Configuration["urls"] ?? app.Configuration.GetSection("ApplicationUrl").Value;

        if (!string.IsNullOrEmpty(urls))
        {
            await ProcessUtils.KillProcessesUsingUrls(urls);
        }
    }

 
    public static async Task SeedRolesAndUsersAsync<TUser, TRole>(this IServiceProvider serviceProvider)
        where TUser : IdentityUser, new()
        where TRole : IdentityRole, new()
    {
        using var scope = serviceProvider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<TRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<TUser>>();

        var roles = UserRoles.All;
        foreach (var roleName in roles)
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

    private static async Task PublishContentTypeChangesAsync<TDbContext>(IServiceProvider serviceProvider)
        where TDbContext : DappiDbContext
    {
        try
        {
            var dbContext = serviceProvider.GetRequiredService<TDbContext>();
            await dbContext.ContentTypeChanges
                .Where(ctc => !ctc.IsPublished)
                .ExecuteUpdateAsync(setters => setters.SetProperty(e => e.IsPublished, true));
        }
        catch (Exception ex)
        {
            var logger = serviceProvider.GetRequiredService<ILogger<TDbContext>>();
            logger.LogError("Error publishing content type changes: {PublishContentChangesError}", ex);
            throw;
        }
    }
}