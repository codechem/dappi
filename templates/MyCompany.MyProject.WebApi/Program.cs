using CCApi.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using MyCompany.MyProject.WebApi.Data;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddOpenApi();
        builder.Services.AddDappi<AppDbContext>(builder.Configuration);
        builder.Services.AddDappiAuthentication<DappiUser, DappiRole, AppDbContext>(builder.Configuration);

        var app = builder.Build();

        app.UseDappi<AppDbContext>();
        app.UseHttpsRedirection();
        app.MapControllers();
        using (var scope = app.Services.CreateScope())
        {
            await SeedRolesAsync(scope.ServiceProvider);
        }

        app.Run();
    }

    static async Task SeedRolesAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<DappiRole>>();

        foreach (var role in new[] { "Admin", "Maintainer", "User" })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new DappiRole { Name = role });
            }
        }
    }
}