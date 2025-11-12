using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Dappi.HeadlessCms.Core;
using Dappi.TestEnv;
using Dappi.TestEnv.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;

namespace Dappi.HeadlessCms.Tests
{
    public class IntegrationWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
    {
        private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
            .WithUsername("postgres")
            .WithPassword("admin")
            .WithDatabase("MyCompany.MyProject.DB")
            .Build();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Test");
            builder.ConfigureServices(services =>
            {
                var existingDbContext = services.SingleOrDefault(x => x.ServiceType == typeof(TestDbContext));
                if (existingDbContext != null)
                    services.Remove(existingDbContext);

                var existingDomainModelEditor =
                    services.SingleOrDefault(x => x.ServiceType == typeof(DomainModelEditor));
                if (existingDomainModelEditor != null)
                    services.Remove(existingDomainModelEditor);

                var existingDbContextEditor = services.SingleOrDefault(x => x.ServiceType == typeof(DbContextEditor));
                if (existingDbContextEditor != null)
                    services.Remove(existingDbContextEditor);

                services.AddDbContext<TestDbContext>(options =>
                {
                    options.UseNpgsql(_dbContainer.GetConnectionString(), b => b.MigrationsAssembly("Dappi.TestEnv"));
                });
            });

            builder.ConfigureServices(services =>
            {
                services.AddControllers();
            });
        }

        public async Task InitializeAsync()
        {
            await _dbContainer.StartAsync();
        }

        public new async Task DisposeAsync()
        {
            await _dbContainer.StopAsync();
        }
    }
}