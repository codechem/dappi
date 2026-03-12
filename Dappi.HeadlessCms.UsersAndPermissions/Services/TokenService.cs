using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Dappi.HeadlessCms.UsersAndPermissions.Api;
using Dappi.HeadlessCms.UsersAndPermissions.Core;
using Dappi.HeadlessCms.UsersAndPermissions.Database;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Dappi.HeadlessCms.UsersAndPermissions.Services;

public class TokenService<TUser>(
    UserManager<TUser> userManager,
    IDbContextAccessor dbContextAccessor,
    IConfiguration config
)
    where TUser : AppUser
{
    private readonly UsersAndPermissionsDbContext _db = dbContextAccessor.DbContext;

    public async Task<AuthResult> GenerateTokens(TUser user)
    {
        var accessToken = GenerateAccessToken(user);
        var refreshToken = await GenerateRefreshToken(user);
        return new AuthResult(accessToken, refreshToken);
    }

    private string GenerateAccessToken(TUser user)
    {
        var jwtSettings = config.GetSection(UsersAndPermissionsConstants.ConfigurationKey);
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(
                jwtSettings["SecretKey"]
                    ?? throw new InvalidOperationException("JWT SecretKey is not configured.")
            )
        );
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, user.Email!),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
        };
        claims.AddRange(new Claim(ClaimTypes.Role, user.Role?.Name!));

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(Convert.ToDouble(jwtSettings["ExpirationHours"])),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task<string> GenerateRefreshToken(TUser user)
    {
        var existing = await _db
            .RefreshTokens.Where(t => t.UserId == user.Id && !t.IsRevoked)
            .ToListAsync();
        foreach (var t in existing)
            t.IsRevoked = true;

        var token = new RefreshToken
        {
            Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
            UserId = user.Id,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
        };

        await _db.RefreshTokens.AddAsync(token);
        await _db.SaveChangesAsync();
        return token.Token;
    }

    public async Task<AuthResult?> RefreshAsync(string refreshToken)
    {
        var stored = await _db.RefreshTokens.FirstOrDefaultAsync(t =>
            t.Token == refreshToken && !t.IsRevoked
        );

        if (stored is null || stored.ExpiresAt < DateTime.UtcNow)
            return null;

        var user = await userManager.FindByIdAsync(stored.UserId.ToString());
        if (user is null)
            return null;

        stored.IsRevoked = true;
        stored.ReplacedByToken = "pending";
        await _db.SaveChangesAsync();

        var result = await GenerateTokens(user);

        stored.ReplacedByToken = result.RefreshToken;
        await _db.SaveChangesAsync();

        return result;
    }
}
