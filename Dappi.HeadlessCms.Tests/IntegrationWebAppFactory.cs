using Dappi.HeadlessCms.Core;
using Dappi.HeadlessCms.Database;
using Dappi.HeadlessCms.Tests.Controllers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Testcontainers.PostgreSql;

namespace Dappi.HeadlessCms.Tests
{
    public class IntegrationWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
    {
        public string TempDir { get; set; }
        private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
            .WithUsername("postgres")
            .WithPassword("admin")
            .WithDatabase("MyCompany.MyProject.DB")
            .Build();
       
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            TempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            builder.UseEnvironment("Test");
            builder.ConfigureServices(services =>
            {
            var existingDbContext = services.SingleOrDefault(x => x.ServiceType == typeof(DbContextOptions<DappiDbContext>));
            if(existingDbContext != null)
                services.Remove(existingDbContext);
            var existingDomainModelEditor = services.SingleOrDefault(x => x.ServiceType == typeof(DomainModelEditor));
            if(existingDomainModelEditor != null)
                services.Remove(existingDomainModelEditor);
            var existingDbContextEditor = services.SingleOrDefault(x => x.ServiceType == typeof(DbContextEditor));
            if(existingDbContextEditor != null)
                services.Remove(existingDbContextEditor);
            
            services.AddDbContext<DappiDbContext>(options =>
            {
                options.UseNpgsql(_dbContainer.GetConnectionString());
            });
            
            services.AddScoped<DbContextEditor>((_) => new DbContextEditor(
                Path.Combine(TempDir, "Data"), "TestDbContext"));
            
            services.AddScoped<DomainModelEditor>(_ => new DomainModelEditor(TempDir));
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