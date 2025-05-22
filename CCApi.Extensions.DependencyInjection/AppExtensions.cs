using CCApi.Extensions.DependencyInjection;
using CCApi.Extensions.DependencyInjection.Constants;
using CCApi.Extensions.DependencyInjection.Database;
using CCApi.Extensions.DependencyInjection.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Hosting;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace Microsoft.Extensions.DependencyInjection;

public static class AppExtensions
{
    public static async Task<IApplicationBuilder> UseDappi<TDbContext>(this WebApplication app,
        Action<SwaggerUIOptions>? configureSwagger = null)
       where TDbContext : DappiDbContext
    {
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

        var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TDbContext>().Database;

        await db.MigrateAsync();
        await app.Services.SeedRolesAndUsersAsync<DappiUser, DappiRole>();

        await PublishContentTypeChangesAsync<TDbContext>(scope.ServiceProvider);

        return app;
    }

    public static async Task SeedRolesAndUsersAsync<TUser, TRole>(this IServiceProvider serviceProvider)
        where TUser : IdentityUser, new()
        where TRole : IdentityRole, new()
    {
        using var scope = serviceProvider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<TRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<TUser>>();

        string[] roles = UserRoles.All;
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
    private static async Task CheckAndCreateContentTypeChangesTableAsync<TDbContext>(IServiceProvider serviceProvider)
    where TDbContext : DbContext
    {
        try
        {
            var dbContext = serviceProvider.GetRequiredService<TDbContext>();

            var modelExists = dbContext.Model.FindEntityType(typeof(ContentTypeChange)) != null;

            if (!modelExists)
            {
                var databaseCreator = dbContext.Database.GetService<IRelationalDatabaseCreator>();

                bool tableExists = false;
                try
                {
                    await dbContext.Database.ExecuteSqlRawAsync(@"SELECT 1 FROM ""ContentTypeChanges"" WHERE 1=0");
                    tableExists = true;
                }
                catch
                {
                    tableExists = false;
                }

                if (!tableExists)
                {
                    await dbContext.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE ""ContentTypeChanges"" (
                    ""Id"" SERIAL PRIMARY KEY,
                    ""ModelName"" VARCHAR(255) NOT NULL,
                    ""Fields"" TEXT NOT NULL,
                    ""ModifiedBy"" VARCHAR(450) NULL,
                    ""ModifiedAt"" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    ""IsPublished"" BOOLEAN NOT NULL DEFAULT FALSE
                )");

                    Console.WriteLine("ContentTypeChanges table created successfully.");
                }
            }
            else
            {
                Console.WriteLine("ContentTypeChanges entity is part of the data model.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error checking/creating ContentTypeChanges table: {ex.Message}");
            throw;
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
            Console.WriteLine($"Error publishing content type changes: {ex.Message}");
            throw;
        }
    }
}