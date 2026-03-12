using System.Text;
using System.Text.Json;
using Dappi.Core.Abstractions.Auth;
using Dappi.HeadlessCms.UsersAndPermissions.Core;
using Dappi.HeadlessCms.UsersAndPermissions.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Dappi.HeadlessCms.UsersAndPermissions.Api
{
    public class SystemJwtValidationProvider<TUser>(IConfiguration configuration)
        : JwtValidationProvider<TUser>
        where TUser : AppUser, new()
    {
        public override SchemaAndIssuerProvider SchemaAndIssuerProvider { get; } =
            new()
            {
                Schema = UsersAndPermissionsConstants.SystemId,
                Issuer =
                    configuration.GetValue<string>(
                        $"{UsersAndPermissionsConstants.ConfigurationKey}:Issuer"
                    ) ?? throw new InvalidOperationException("JWT Issuer is not configured."),
            };

        public override TokenValidationParameters BuildValidationParameters()
        {
            var configurationSection = configuration.GetSection(
                UsersAndPermissionsConstants.ConfigurationKey
            );
            var secretKey =
                configurationSection["SecretKey"]
                ?? throw new InvalidOperationException("EndUser JWT SecretKey is not configured");
            var key = Encoding.UTF8.GetBytes(secretKey);

            return new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = configurationSection["Issuer"],
                ValidAudience = configurationSection["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ClockSkew = TimeSpan.Zero,
            };
        }

        public JwtBearerEvents BuildEvents()
        {
            return new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    if (context.Exception is SecurityTokenExpiredException)
                        context.Response.Headers.Append("Token-Expired", "true");
                    return Task.CompletedTask;
                },
                OnChallenge = context =>
                {
                    context.HandleResponse();
                    context.Response.StatusCode = 401;
                    context.Response.ContentType = "application/json";
                    var result = JsonSerializer.Serialize(new { error = "You are not authorized" });
                    return context.Response.WriteAsync(result);
                },
            };
        }
    }
}
