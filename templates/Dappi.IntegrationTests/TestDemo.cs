using Dappi.HeadlessCms.Controllers;
using Dappi.HeadlessCms.Core;
using Dappi.HeadlessCms.Interfaces;
using Dappi.HeadlessCms.Models;
using Dappi.HeadlessCms.Services;
using Microsoft.AspNetCore.Http;
using MyCompany.MyProject.WebApi.Data;

namespace Dappi.IntegrationTests
{
    public class TestDemo : BaseIntegrationTest
    {
        readonly string _tempDir;
        readonly ModelsController _controller;
        // public TestDemo(IntegrationWebAppFactory factory)
        // {
        //     var dbContextEditor = factory.Services.GetService(typeof(DbContextEditor));
        //     var domainModelEditor = factory.Services.GetService(typeof(DomainModelEditor));
        //     var dbContext = factory.Services.GetService(typeof(AppDbContext));
        //     IDbContextAccessor accessor = new DbContextAccessor<AppDbContext>((AppDbContext)dbContext);
        //     ICurrentDappiSessionProvider sessionProvider = new CurrentDappiSessionProvider(new HttpContextAccessor());
        //     _controller = new ModelsController(accessor,sessionProvider, (DomainModelEditor)domainModelEditor, (DbContextEditor)dbContextEditor);
        //     _tempDir = factory.TempDir;
        // }

        public TestDemo(IntegrationWebAppFactory factory) : base(factory)
        {
            IDbContextAccessor accessor = new DbContextAccessor<AppDbContext>(DbContext);
            ICurrentDappiSessionProvider sessionProvider = new CurrentDappiSessionProvider(new HttpContextAccessor());
            _controller = new ModelsController(accessor,sessionProvider, DomainModelEditor, DbContextEditor);
        }

        [Fact]
        public async Task CreateModel()
        {
            var request = new ModelRequest{ ModelName = "Product", IsAuditableEntity = false };
            await _controller.CreateModel(request);
            
            var actual = await File.ReadAllTextAsync(Path.Combine(TempDir, "Product.cs"));
            
            Assert.Contains("public class Product", actual);
        }
    }
}