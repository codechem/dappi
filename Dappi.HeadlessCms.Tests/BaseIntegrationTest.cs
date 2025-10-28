using System.Linq;
using Dappi.HeadlessCms.Core;
using Dappi.TestEnv.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Dappi.HeadlessCms.Tests
{
    public class BaseIntegrationTest : IClassFixture<IntegrationWebAppFactory>
    {
        protected readonly TestDbContext DbContext;
        public BaseIntegrationTest(IntegrationWebAppFactory factory)
        { 
            DbContext = factory.Services.GetRequiredService<TestDbContext>();
            if (DbContext.Database.GetPendingMigrations().Any())
            {
                DbContext.Database.MigrateAsync();
            }
        }
    }
}