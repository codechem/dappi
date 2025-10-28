using Dappi.HeadlessCms.Core;
using Dappi.HeadlessCms.Interfaces;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyCompany.MyProject.WebApi.Data;

namespace Dappi.IntegrationTests
{
    public class BaseIntegrationTest : IClassFixture<IntegrationWebAppFactory>
    {
        protected readonly AppDbContext DbContext;
        protected readonly DomainModelEditor DomainModelEditor;
        protected readonly DbContextEditor DbContextEditor;
        protected readonly string TempDir;
        public BaseIntegrationTest(IntegrationWebAppFactory factory)
        {
            TempDir = factory.TempDir;
            DbContext = factory.Services.GetRequiredService<AppDbContext>();
            DomainModelEditor = factory.Services.GetRequiredService<DomainModelEditor>();
            DbContextEditor = factory.Services.GetRequiredService<DbContextEditor>();

            if (DbContext.Database.GetPendingMigrations().Any())
            {
                DbContext.Database.Migrate();
            }
        }
    }
}