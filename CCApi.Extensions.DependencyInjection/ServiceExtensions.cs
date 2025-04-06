using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.OpenApi.Models;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceExtensions
{
    public static IServiceCollection AddDappi<TDbContext>(
        this IServiceCollection services, 
        IConfiguration configuration,
        Action<JsonOptions>? jsonOptions = null,
        Action<DbContextOptionsBuilder>? dbContextOptions = null)
        where TDbContext : DbContext
    {
        services.AddDbContext<TDbContext>(dbContextOptions ?? (builder =>
            builder.UseNpgsql(configuration.GetValue<string>(Constants.Configuration.PostgresConnection))));
#if DEBUG
        services.AddDappiSwaggerGen();
#endif
        services.AddControllers()
            .AddJsonOptions(jsonOptions ?? (options =>
            {
                options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            }) );
        
        services.AddEndpointsApiExplorer();
        
        if (configuration.IsDappiUiConfigured())
        {
            services.AddCors(options =>
            {
                options.AddPolicy(Constants.CorsPolicies.AllowDappiAngularApp,
                    policy => policy.WithOrigins(configuration.GetValue<string>(Constants.Configuration.FrontendUrl)!)
                        .AllowAnyHeader()
                        .AllowAnyMethod());
            });
        }
        
        return services;
    }
    
    private static IServiceCollection AddDappiSwaggerGen(this IServiceCollection services)
    {
        return services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("Toolkit", new OpenApiInfo { Title = "Toolkit API", Version = "v1" });
            c.SwaggerDoc("Default", new OpenApiInfo { Title = "Default API", Version = "v1" });

            c.DocInclusionPredicate((docName, apiDesc) =>
            {
                if (apiDesc.GroupName == null) return docName == "Default";
                return docName == apiDesc.GroupName;
            });

            c.TagActionsBy(api => api.GroupName ?? api.ActionDescriptor.RouteValues["controller"]);
        });
    }
}