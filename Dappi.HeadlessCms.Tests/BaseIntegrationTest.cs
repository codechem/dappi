using Dappi.HeadlessCms.Core;
using Dappi.HeadlessCms.Database;
using Dappi.HeadlessCms.Interfaces;
using Dappi.HeadlessCms.Tests;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
// using MyCompany.MyProject.WebApi.Data;

namespace Dappi.IntegrationTests
{
    public class BaseIntegrationTest : IClassFixture<IntegrationWebAppFactory>
    {
        protected readonly DappiDbContext DbContext;
        protected readonly DomainModelEditor DomainModelEditor;
        protected readonly DbContextEditor DbContextEditor;
        protected readonly string TempDir;
        public BaseIntegrationTest(IntegrationWebAppFactory factory)
        { 
            TempDir = factory.TempDir;
            DbContext = factory.Services.GetRequiredService<DappiDbContext>();
            DomainModelEditor = factory.Services.GetRequiredService<DomainModelEditor>();
            DbContextEditor = factory.Services.GetRequiredService<DbContextEditor>();

            if (DbContext.Database.GetPendingMigrations().Any())
            {
                DbContext.Database.Migrate();
            }
        }
    }
}