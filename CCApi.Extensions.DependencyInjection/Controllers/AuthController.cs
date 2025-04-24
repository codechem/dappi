using CCApi.Extensions.DependencyInjection.Services.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CCApi.Extensions.DependencyInjection.Controllers;

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
public class ModelFieldsController : ControllerBase
{
    private readonly ILogger<ModelFieldsController> _logger;

    public ModelFieldsController(ILogger<ModelFieldsController> logger)
    {
        _logger = logger;
    }

    [HttpGet("Users")]
    [Authorize(Roles = "Admin")]
    public IActionResult GetUserFields()
    {
        try
        {
            var fields = new List<object>
        {
            new { fieldName = "id", fieldType = "Guid", isRequired = false },
            new { fieldName = "email", fieldType = "string", isRequired = false },
            new { fieldName = "name", fieldType = "string", isRequired = false },
            new { fieldName = "roles", fieldType = "userRoles", isRequired = false }
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

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly UserManager<DappiUser> _userManager;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        UserManager<DappiUser> userManager,
        ILogger<UsersController> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public IActionResult GetUsers([FromQuery] int offset = 0, [FromQuery] int limit = 10, [FromQuery] string searchTerm = "")
    {
        try
        {
            var query = _userManager.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                query = query.Where(u =>
                    u.UserName.ToLower().Contains(searchTerm) ||
                    u.Email.ToLower().Contains(searchTerm));
            }

            var totalCount = query.Count();
            var users = query.Skip(offset).Take(limit).ToList();

            var result = new List<UserRoleDto>();

            foreach (var user in users)
            {
                var roles = _userManager.GetRolesAsync(user).Result;

                result.Add(new UserRoleDto
                {
                    Id = user.Id,
                    Name = user.UserName,
                    Email = user.Email,
                    Roles = roles.ToList()
                });
            }

            var response = new PagedResponseDto<UserRoleDto>
            {
                Total = totalCount,
                Offset = offset,
                Limit = limit,
                Data = result
            };

            _logger.LogInformation("Retrieved {Count} users out of {Total}", result.Count, totalCount);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving users: {Message}", ex.Message);
            return StatusCode(500, new { message = "An error occurred while retrieving users" });
        }
    }

    [HttpGet("username/{username}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetUserByUsername(string username)
    {
        try
        {
            var user = await _userManager.FindByNameAsync(username);

            if (user == null)
            {
                _logger.LogWarning("User with username {Username} not found", username);
                return NotFound(new { message = "User not found" });
            }

            var roles = await _userManager.GetRolesAsync(user);

            var userDto = new UserRoleDto
            {
                Id = user.Id,
                Name = user.UserName,
                Email = user.Email,
                Roles = roles.ToList()
            };

            _logger.LogInformation("Retrieved user {Username}", username);
            return Ok(userDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user with username {Username}: {Message}", username, ex.Message);
            return StatusCode(500, new { message = "An error occurred while retrieving the user" });
        }
    }

    [HttpPut("{username}/roles")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateUserRoles(string username, [FromBody] UserRolesUpdateDto dto)
    {
        try
        {
            var user = await _userManager.FindByNameAsync(username);
            if (user == null)
            {
                _logger.LogWarning("User with username {Username} not found", username);
                return NotFound(new { message = "User not found" });
            }

            var currentRoles = await _userManager.GetRolesAsync(user);

            var rolesToRemove = currentRoles.Where(r => !dto.Roles.Contains(r)).ToArray();
            if (rolesToRemove.Any())
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
                if (!removeResult.Succeeded)
                {
                    _logger.LogWarning("Failed to remove roles from user {Username}: {Errors}",
                        user.UserName, string.Join(", ", removeResult.Errors.Select(e => e.Description)));
                    return BadRequest(removeResult.Errors);
                }
            }

            var rolesToAdd = dto.Roles.Where(r => !currentRoles.Contains(r)).ToArray();
            if (rolesToAdd.Any())
            {
                var addResult = await _userManager.AddToRolesAsync(user, rolesToAdd);
                if (!addResult.Succeeded)
                {
                    _logger.LogWarning("Failed to add roles to user {Username}: {Errors}",
                        user.UserName, string.Join(", ", addResult.Errors.Select(e => e.Description)));
                    return BadRequest(addResult.Errors);
                }
            }

            var updatedRoles = await _userManager.GetRolesAsync(user);

            _logger.LogInformation("Updated roles for user {Username}", user.UserName);
            return Ok(new
            {
                message = "User roles updated successfully",
                roles = updatedRoles
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating roles for user {Username}: {Message}", username, ex.Message);
            return StatusCode(500, new { message = "An error occurred while updating user roles" });
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateUser(string id, [FromBody] UserDto dto)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                _logger.LogWarning("User with ID {UserId} not found", id);
                return NotFound(new { message = "User not found" });
            }

            user.Email = dto.Email;
            user.UserName = dto.Name;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                _logger.LogWarning("Failed to update user {UserId}: {Errors}",
                    id, string.Join(", ", updateResult.Errors.Select(e => e.Description)));
                return BadRequest(updateResult.Errors);
            }

            var currentRoles = await _userManager.GetRolesAsync(user);

            List<string> newRoles;
            if (dto.Roles.Count == 1 && dto.Roles[0].Contains(","))
            {
                newRoles = dto.Roles[0].Split(',').Select(r => r.Trim()).ToList();
            }
            else
            {
                newRoles = dto.Roles;
            }

            var rolesToRemove = currentRoles.Where(r => !newRoles.Contains(r)).ToArray();
            if (rolesToRemove.Any())
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
                if (!removeResult.Succeeded)
                {
                    _logger.LogWarning("Failed to remove roles from user {UserId}: {Errors}",
                        id, string.Join(", ", removeResult.Errors.Select(e => e.Description)));
                    return BadRequest(removeResult.Errors);
                }
            }

            var rolesToAdd = newRoles.Where(r => !currentRoles.Contains(r)).ToArray();
            if (rolesToAdd.Any())
            {
                var addResult = await _userManager.AddToRolesAsync(user, rolesToAdd);
                if (!addResult.Succeeded)
                {
                    _logger.LogWarning("Failed to add roles to user {UserId}: {Errors}",
                        id, string.Join(", ", addResult.Errors.Select(e => e.Description)));
                    return BadRequest(addResult.Errors);
                }
            }

            var updatedRoles = await _userManager.GetRolesAsync(user);

            _logger.LogInformation("Updated user {UserId}", id);
            return Ok(new
            {
                id = user.Id,
                name = user.UserName,
                email = user.Email,
                roles = updatedRoles
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId}: {Message}", id, ex.Message);
            return StatusCode(500, new { message = "An error occurred while updating the user" });
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteUser(string id)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                _logger.LogWarning("User with ID {UserId} not found", id);
                return NotFound(new { message = "User not found" });
            }

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                _logger.LogWarning("Failed to delete user {UserId}: {Errors}",
                    id, string.Join(", ", result.Errors.Select(e => e.Description)));
                return BadRequest(result.Errors);
            }

            _logger.LogInformation("Deleted user {UserId}", id);
            return Ok(new { message = "User deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {UserId}: {Message}", id, ex.Message);
            return StatusCode(500, new { message = "An error occurred while deleting the user" });
        }
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

public class UserDto
{
    public string Email { get; set; }
    public string Name { get; set; }
    public List<string> Roles { get; set; } = new List<string>();
}

public class UserRoleDto
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public List<string> Roles { get; set; }
}

public class RoleDto
{
    public string Id { get; set; }
    public string Name { get; set; }
}

public class PagedResponseDto<T>
{
    public int Total { get; set; }
    public int Offset { get; set; }
    public int Limit { get; set; }
    public List<T> Data { get; set; }
}

public class UserRoleUpdateDto
{
    public string Role { get; set; }
}

public class UserRolesUpdateDto
{
    public List<string> Roles { get; set; }
}