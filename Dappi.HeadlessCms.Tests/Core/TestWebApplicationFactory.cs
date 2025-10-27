using Dappi.HeadlessCms.Tests.Controllers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Dappi.HeadlessCms.Tests.Core
{
    public class TestWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Add controllers from the test assembly
                var mvcBuilder = services.AddControllers();
                mvcBuilder.PartManager.ApplicationParts.Add(
                    new Microsoft.AspNetCore.Mvc.ApplicationParts.AssemblyPart(
                        typeof(ModelsControllerTests).Assembly));
            });

            builder.Configure(app =>
            {
                app.UseRouting();
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapControllers();
                });
            });
        }
    }
}