using Dappi.HeadlessCms;
using Dappi.HeadlessCms.Database;
using Dappi.HeadlessCms.Models;
using Dappi.TestEnv.Data;
using Microsoft.AspNetCore.Builder;

namespace Dappi.TestEnv
{
    public class Program
    {
        private static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddDappi<TestDbContext>(builder.Configuration);
            builder.Services.AddDappiAuthentication<DappiUser, DappiRole, TestDbContext>(builder.Configuration);

            var app = builder.Build();

            await app.UseDappi<TestDbContext>();
        
            app.UseHttpsRedirection();
            app.MapControllers();
        
            app.Run();
        }
    }
}