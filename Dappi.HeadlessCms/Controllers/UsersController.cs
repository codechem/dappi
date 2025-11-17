using Dappi.HeadlessCms.Authentication;
using Dappi.HeadlessCms.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Dappi.HeadlessCms.Controllers
{
    [ApiController]
    [Route("api/users")]
    [Authorize(Policy = DappiAuthenticationSchemes.DappiAuthenticationScheme, Roles = "Admin")]
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
        public IActionResult GetUsers([FromQuery] int offset = 0, [FromQuery] int limit = 10, [FromQuery] string searchTerm = "")
        {
            try
            {
                var query = _userManager.Users.AsQueryable();

                searchTerm = searchTerm.Trim().ToLower();

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
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
}