using System.Text;
using System.Text.Json.Serialization;
using CCApi.Extensions.DependencyInjection.Database;
using CCApi.Extensions.DependencyInjection.Interfaces;
using CCApi.Extensions.DependencyInjection.Services;
using CCApi.Extensions.DependencyInjection.Services.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceExtensions
{
    public static IServiceCollection AddDappi<TDbContext>(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<JsonOptions>? jsonOptions = null,
        Action<DbContextOptionsBuilder>? dbContextOptions = null)
        where TDbContext : DappiDbContext
    {
        services.AddDbContext<TDbContext>(dbContextOptions ?? (builder =>
            builder.UseNpgsql(configuration.GetValue<string>(Constants.Configuration.PostgresConnection))));

        services.AddScoped<IDbContextAccessor, DbContextAccessor<TDbContext>>();

        services.AddTransient<IMediaUploadService, LocalStorageUploadService>();

        services.AddScoped<ICurrentSessionProvider, CurrentSessionProvider>();

        services.AddDappiSwaggerGen();

        services.AddControllers()
            .AddJsonOptions(jsonOptions ?? (options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = null;
                options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            }));

        services.AddEndpointsApiExplorer();

        return services;
    }

    public static IServiceCollection AddDappiAuthentication<TUser, TRole, TContext>(
    this IServiceCollection services,
    IConfiguration configuration)
    where TUser : IdentityUser, new()
    where TRole : IdentityRole, new()
    where TContext : DbContext
    {
        services.AddIdentity<TUser, TRole>(options =>
        {
            options.User.RequireUniqueEmail = true;

            // Password settings
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequireUppercase = true;
            options.Password.RequiredLength = 8;

            // Lockout settings
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;
        })
        .AddEntityFrameworkStores<TContext>()
        .AddDefaultTokenProviders();

        // JWT Authentication
        var jwtSettings = configuration.GetSection("JwtSettings");
        var key = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"] ??
            throw new InvalidOperationException("JWT SecretKey is not configured"));

        services.AddAuthentication(options =>
    {
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
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
                    context.Response.Headers.Add("Token-Expired", "true");
                }
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";

                var result = System.Text.Json.JsonSerializer.Serialize(new { error = "You are not authorized" });
                return context.Response.WriteAsync(result);
            }
        };
    });

        services.AddScoped<TokenService<TUser>>();
        services.AddAuthorization();

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

            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
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
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    new string[] { }
                }
            });

            c.TagActionsBy(api => api.GroupName ?? api.ActionDescriptor.RouteValues["controller"]);
        });
    }
}