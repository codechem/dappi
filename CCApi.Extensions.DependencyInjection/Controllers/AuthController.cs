using CCApi.Extensions.DependencyInjection.Services.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace CCApi.Extensions.DependencyInjection.Controllers;

[ApiController]
[Route("api/[controller]")]
public partial class AuthController : ControllerBase
{
    private readonly UserManager<DappiUser> _userManager;
    private readonly SignInManager<DappiUser> _signInManager;
    private readonly TokenService<DappiUser> _tokenService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        UserManager<DappiUser> userManager,
        SignInManager<DappiUser> signInManager,
        TokenService<DappiUser> tokenService,
        ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = new DappiUser { UserName = dto.Username, Email = dto.Email };
        var result = await _userManager.CreateAsync(user, dto.Password);

        if (!result.Succeeded)
        {
            _logger.LogWarning("User registration failed: {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Description)));
            return BadRequest(result.Errors);
        }

        await _userManager.AddToRoleAsync(user, "User"); // Default role
        _logger.LogInformation("User {Username} registered successfully", dto.Username);

        return Ok(new { message = "Registration successful" });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await _userManager.FindByNameAsync(dto.Username);

        if (user == null)
        {
            _logger.LogWarning("Login failed: User {Username} not found", dto.Username);
            return Unauthorized(new { message = "Invalid username or password" });
        }

        var signInResult = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, lockoutOnFailure: true);

        if (!signInResult.Succeeded)
        {
            if (signInResult.IsLockedOut)
            {
                _logger.LogWarning("Account locked out for user {Username}", dto.Username);
                return StatusCode(423, new { message = "Account is locked. Try again later." });
            }

            _logger.LogWarning("Login failed: Invalid password for user {Username}", dto.Username);
            return Unauthorized(new { message = "Invalid username or password" });
        }

        var token = await _tokenService.GenerateJwtToken(user);
        var roles = await _userManager.GetRolesAsync(user);

        _logger.LogInformation("User {Username} logged in successfully", dto.Username);

        return Ok(new
        {
            token,
            username = user.UserName,
            roles
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
