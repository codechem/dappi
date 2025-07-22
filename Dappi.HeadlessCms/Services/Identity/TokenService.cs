using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Dappi.HeadlessCms.Services.Identity
{
    public class TokenService<TUser> where TUser : IdentityUser
    {
        private readonly IConfiguration _config;
        private readonly UserManager<TUser> _userManager;

        public TokenService(IConfiguration config, UserManager<TUser> userManager)
        {
            _config = config;
            _userManager = userManager;
        }

        public async Task<string> GenerateJwtToken(TUser user)
        {
            var jwtSettings = _config.GetSection("JwtSettings");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var roles = await _userManager.GetRolesAsync(user);

            var claims = new List<Claim> {
                new(ClaimTypes.Name, user.UserName),
                new(ClaimTypes.NameIdentifier, user.Id)
            };
            claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(Convert.ToDouble(jwtSettings["ExpirationHours"])),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}