using CCApi.Extensions.DependencyInjection;
using MyCompany.MyProject.WebApi.Data;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddDappi<AppDbContext>(builder.Configuration);
        builder.Services.AddDappiAuthentication<DappiUser, DappiRole, AppDbContext>(builder.Configuration);

        var app = builder.Build();

        await app.UseDappi<AppDbContext>();
        
        app.UseHttpsRedirection();
        app.MapControllers();
        
        app.Run();
    }
}