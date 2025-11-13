using Dappi.TestEnv.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Dappi.HeadlessCms.Tests
{
    public class BaseIntegrationTestFixture : IClassFixture<IntegrationWebAppFactory>
    {
        protected readonly TestDbContext DbContext;
        public BaseIntegrationTestFixture(IntegrationWebAppFactory factory)
        { 
            DbContext = factory.Services.GetRequiredService<TestDbContext>();
            if (DbContext.Database.GetPendingMigrations().Any())
            {
                DbContext.Database.MigrateAsync();
            }
        }
    }
}