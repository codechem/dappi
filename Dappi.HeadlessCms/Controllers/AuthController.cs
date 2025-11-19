using Dappi.HeadlessCms.Authentication;
using Dappi.HeadlessCms.Models;
using Dappi.HeadlessCms.Services.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Dappi.HeadlessCms.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
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

[ApiController]
[Route("api/models/fields")]
[Authorize(Policy = DappiAuthenticationSchemes.DappiAuthenticationScheme, Roles = "Admin")]
public class ModelFieldsController : ControllerBase
{
    private readonly ILogger<ModelFieldsController> _logger;

    public ModelFieldsController(ILogger<ModelFieldsController> logger)
    {
        _logger = logger;
    }

    [HttpGet("Users")]
    public IActionResult GetUserFields()
    {
        try
        {
            var fields = new List<object>
        {
            new { fieldName = "Id", fieldType = "Guid", isRequired = false },
            new { fieldName = "Email", fieldType = "string", isRequired = false },
            new { fieldName = "Name", fieldType = "string", isRequired = false },
            new { fieldName = "Roles", fieldType = "userRoles", isRequired = false }
        };

            _logger.LogInformation("Retrieved user field names");
            return Ok(fields);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user field names: {Message}", ex.Message);
            return StatusCode(500, new { message = "An error occurred while retrieving user field names" });
        }
    }

}