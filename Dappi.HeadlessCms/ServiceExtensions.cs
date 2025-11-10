using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using Dappi.HeadlessCms.Authentication;
using Dappi.HeadlessCms.Database;
using Dappi.HeadlessCms.Database.Interceptors;
using Dappi.HeadlessCms.Interfaces;
using Dappi.HeadlessCms.Services;
using Dappi.HeadlessCms.Services.Identity;
using Dappi.HeadlessCms.Core;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Npgsql;
using CCApi.Extensions.DependencyInjection.Interfaces;
using CCApi.Extensions.DependencyInjection.Services;

namespace Dappi.HeadlessCms;

public static class ServiceExtensions
{
    public static IServiceCollection AddDappi<TDbContext>(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<JsonOptions>? jsonOptions = null)
        where TDbContext : DappiDbContext
    {
        services.AddScoped<AuditTrailInterceptor>();

        services.AddDbContext<TDbContext>((provider, options) =>
        {
            var dataSourceBuilder =
                new NpgsqlDataSourceBuilder(configuration.GetValue<string>(Constants.Configuration.PostgresConnection));
            dataSourceBuilder.EnableDynamicJson();

            var interceptor = provider.GetRequiredService<AuditTrailInterceptor>();

            options.UseNpgsql(dataSourceBuilder.Build())
                .AddInterceptors(interceptor);
        });

        services.AddScoped<IDbContextAccessor, DbContextAccessor<TDbContext>>();
        services.AddScoped<IEnumService, EnumService>();

        services.AddTransient<IMediaUploadService, LocalStorageUploadService>();

        services.AddHttpContextAccessor();

        services.AddScoped<ICurrentDappiSessionProvider, CurrentDappiSessionProvider>();
        
        services.AddScoped<ICurrentExternalSessionProvider, CurrentExternalSessionProvider>();

        services.AddDappiSwaggerGen();

        services.AddControllers()
            .AddJsonOptions(jsonOptions ?? (options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = null;
                options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            }));
        
        var dbContextType = typeof(TDbContext);
        var location = Assembly.GetAssembly(dbContextType)?.Location;
        
        services.AddScoped<DbContextEditor>((_) => new DbContextEditor(
            Path.Combine(Directory.GetCurrentDirectory(), "Data"),
            dbContextName: dbContextType.Name));
        
        services.AddScoped<DomainModelEditor>(_ => new DomainModelEditor(Path.Combine(
            Directory.GetCurrentDirectory(),
            "Entities"
        )));

        services.AddScoped<EnumEditor>(_ => new EnumEditor(Path.Combine(Directory.GetCurrentDirectory(), "Enums")));
        
        services.AddEndpointsApiExplorer();
        return services;
    }

    public static IServiceCollection AddDappiAuthentication<TUser, TRole, TContext>(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<JwtBearerOptions>? externalJwtBearerOptions = null)
        where TUser : IdentityUser, new()
        where TRole : IdentityRole, new()
        where TContext : DbContext
    {
        services.AddIdentity<TUser, TRole>(options =>
            {
                options.User.RequireUniqueEmail = true;

                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequireUppercase = true;
                options.Password.RequiredLength = 8;

                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;
            })
            .AddEntityFrameworkStores<TContext>()
            .AddDefaultTokenProviders();

        // JWT Authentication
        var jwtSettings = configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey is not configured");
        var key = Encoding.UTF8.GetBytes(secretKey);

        var authenticationBuilder = services.AddAuthentication(options =>
        {
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        });
        
        authenticationBuilder.AddJwtBearer(DappiAuthenticationSchemes.DappiAuthenticationScheme, options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidAudience = jwtSettings["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ClockSkew = TimeSpan.Zero
            };

            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    if (context.Exception is SecurityTokenExpiredException)
                    {
                        context.Response.Headers.Append("Token-Expired", "true");
                    }

                    return Task.CompletedTask;
                },
                OnChallenge = context =>
                {
                    context.HandleResponse();
                    context.Response.StatusCode = 401;
                    context.Response.ContentType = "application/json";

                    var result =
                        System.Text.Json.JsonSerializer.Serialize(new { error = "You are not authorized" });
                    return context.Response.WriteAsync(result);
                }
            };
        });

        if (externalJwtBearerOptions is not null)
        {
            authenticationBuilder.AddJwtBearer(DappiAuthenticationSchemes.ExternalAuthenticationScheme, externalJwtBearerOptions);
        }

        services.AddScoped<TokenService<TUser>>();
        
        services.AddAuthorization(opts =>
        {
            var dappiPolicy = new AuthorizationPolicyBuilder(DappiAuthenticationSchemes.DappiAuthenticationScheme)
                .RequireAuthenticatedUser()
                .Build();
            
            var externalPolicy = new AuthorizationPolicyBuilder(DappiAuthenticationSchemes.ExternalAuthenticationScheme)
                .RequireAuthenticatedUser()
                .Build();
            
            var defaultPolicy = new AuthorizationPolicyBuilder(dappiPolicy).Combine(externalPolicy)
                .Build();  
            
            opts.AddPolicy(DappiAuthenticationSchemes.DappiAuthenticationScheme, dappiPolicy);
            opts.AddPolicy(DappiAuthenticationSchemes.ExternalAuthenticationScheme,  externalPolicy);
            opts.DefaultPolicy = defaultPolicy;
        });

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

            c.AddSecurityDefinition("Bearer",
                new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.ApiKey,
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    Description = "Enter 'Bearer' followed by your token"
                });

            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                    },
                    new string[] { }
                }
            });
            c.TagActionsBy(api => api.GroupName ?? api.ActionDescriptor.RouteValues["controller"]);
        });
    }
}