using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace Microsoft.Extensions.DependencyInjection;

public static class AppExtensions
{
    public static IApplicationBuilder UseDappi<TDbContext>(this WebApplication app, 
        Action<SwaggerUIOptions>? configureSwagger = null, 
        Action<CorsPolicyBuilder>? configureCors = null)
        where TDbContext : DbContext
    {
        if (configureCors is not null)
            app.UseCors(configureCors);

        if (app.Configuration.IsDappiUiConfigured())
            app.UseCors(Constants.CorsPolicies.AllowDappiAngularApp);

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/Toolkit/swagger.json", "Toolkit API v1");
                c.SwaggerEndpoint("/swagger/Default/swagger.json", "Default API v1");
                c.RoutePrefix = string.Empty;
            });
            
            using var scope = app.Services.CreateScope();
            var database = scope.ServiceProvider.GetRequiredService<TDbContext>().Database;
            database.Migrate();
        }
        
        app.UseHttpsRedirection();
        app.MapControllers();

        return app;
    }
}