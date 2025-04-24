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
                c.RoutePrefix = "swagger";
            });

        }

        app.Use(async (context, next) =>
        {
            await next();

            if (!context.Request.Path.Value.StartsWith("/api/") && 
                !Path.HasExtension(context.Request.Path) || 
                context.Response.StatusCode == 404)
            {
                if (!context.Response.HasStarted) 
                {
                   context.Request.Path = "/index.html";
                   await next();
                }
            }
        });

        app.UseHttpsRedirection();
        app.MapControllers();
        app.UseStaticFiles();

        return app;
    }
}