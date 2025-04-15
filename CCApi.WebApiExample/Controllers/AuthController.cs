using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CCApi.WebApiExample.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace CCApi.WebApiExample.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<DappiUser> _userManager;
    private readonly SignInManager<DappiUser> _signInManager;
    private readonly IConfiguration _config;

    public AuthController(UserManager<DappiUser> userManager, SignInManager<DappiUser> signInManager, IConfiguration config)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _config = config;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        var user = new DappiUser { UserName = dto.Username, Email = dto.Email };
        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded) return BadRequest(result.Errors);

        await _userManager.AddToRoleAsync(user, "User"); // Default role
        return Ok();
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var user = await _userManager.FindByNameAsync(dto.Username);
        if (user == null || !await _userManager.CheckPasswordAsync(user, dto.Password))
            return Unauthorized();

        var roles = await _userManager.GetRolesAsync(user);

        var claims = new List<Claim> {
            new Claim(ClaimTypes.Name, user.UserName!),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var token = new JwtSecurityToken(
            issuer: "dappi.net",
            audience: "dappi.net.audience",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes("your_super_secret_keyyour_super_secret_key")),
                SecurityAlgorithms.HmacSha256)
        );

        return Ok(new {
            token = new JwtSecurityTokenHandler().WriteToken(token)
        });
    }
}

public class LoginDto
{
    public required string Username { get; set; }
    public required string Password { get; set; }
}

public class RegisterDto
{
    public string Username { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
}