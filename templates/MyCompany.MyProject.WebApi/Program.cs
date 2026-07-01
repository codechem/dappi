using Dappi.HeadlessCms;
using Dappi.HeadlessCms.Models;
using Dappi.HeadlessCms.UsersAndPermissions;
using GeneratedPermissions;
using MyCompany.MyProject.WebApi.Data;
using MyCompany.MyProject.WebApi.UsersAndPermissionsSystem.Data;
using AppUser = MyCompany.MyProject.WebApi.UsersAndPermissionsSystem.Entities.AppUser;

namespace MyCompany.MyProject.WebApi;

public class Program
{
    private static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddDappi<AppDbContext>(builder.Configuration);
        builder.Services.AddAwsSes(builder.Configuration);

        builder.Services.AddDappiAuthentication<DappiUser, DappiRole, AppDbContext>(builder.Configuration);
        builder.Services.AddUsersAndPermissionsSystem<AppUsersAndPermissionsDbContext, AppUser>(
            PermissionsMeta.Controllers,
            builder.Configuration
        );

        var app = builder.Build();

        await app.UseUsersAndPermissionsSystem<AppUsersAndPermissionsDbContext, AppUser>(
            typeof(Program).Assembly
        );

        await app.UseDappi<AppDbContext>();

        app.Run();
    }
}
