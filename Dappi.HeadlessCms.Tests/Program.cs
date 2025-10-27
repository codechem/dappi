using Dappi.HeadlessCms.Database;
using Dappi.HeadlessCms.Models;
using Microsoft.AspNetCore.Builder;

namespace Dappi.HeadlessCms.Tests
{
    public class Program
    {
        private static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddDappi<DappiDbContext>(builder.Configuration);
            builder.Services.AddDappiAuthentication<DappiUser, DappiRole, DappiDbContext>(builder.Configuration);

            var app = builder.Build();

            await app.UseDappi<DappiDbContext>();
        
            app.UseHttpsRedirection();
            app.MapControllers();
        
            app.Run();
        }
    }
}