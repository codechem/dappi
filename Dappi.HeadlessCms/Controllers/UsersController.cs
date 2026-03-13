using Dappi.HeadlessCms.Authentication;
using Dappi.HeadlessCms.Interfaces;
using Dappi.HeadlessCms.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Dappi.HeadlessCms.Controllers
{
    [ApiController]
    [Route("api/users")]
    [Authorize(Policy = DappiAuthenticationSchemes.DappiAuthenticationScheme, Roles = "Admin")]
    public class UsersController : ControllerBase
    {
        private readonly UserManager<DappiUser> _userManager;
        private readonly ILogger<UsersController> _logger;
        private readonly IEmailService? _emailService;
        private readonly IConfiguration _configuration;
        private readonly IDataProtector _invitationProtector;

        public UsersController(
            UserManager<DappiUser> userManager,
            ILogger<UsersController> logger,
            IConfiguration configuration,
            IDataProtectionProvider dataProtectionProvider,
            IEmailService? emailService = null)
        {
            _userManager = userManager;
            _logger = logger;
            _configuration = configuration;
            _emailService = emailService;
            _invitationProtector = dataProtectionProvider.CreateProtector("Dappi.HeadlessCms.Users.Invitation.v1");
        }

        [HttpPost]
        public async Task<IActionResult> InviteUser([FromBody] InviteUserDto dto)
        {
            var rolesToAssign = dto.Roles.Count > 0 ? dto.Roles : new List<string> { Constants.UserRoles.User };
            var generatedPassword = GeneratePassword();

            var invitationPayload = new InvitationPayload(
                dto.Username,
                dto.Email,
                generatedPassword,
                rolesToAssign,
                DateTime.UtcNow.AddDays(2)
            );

            var protectedToken = _invitationProtector.Protect(JsonSerializer.Serialize(invitationPayload));
            var token = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(protectedToken));

            var acceptancePath = $"/api/users/accept-invitation?token={token}";
            var requestBaseUrl = $"{Request.Scheme}://{Request.Host}";
            var acceptUrl = $"{requestBaseUrl}{acceptancePath}";

            var frontendUrl = _configuration.GetValue<string>(Constants.Configuration.FrontendUrl);
            var frontendAcceptUrl = string.IsNullOrWhiteSpace(frontendUrl) ? null : $"{frontendUrl.TrimEnd('/')}/accept-invitation?token={token}";

            var emailSubject = "You're invited to join Dappi";
            var emailTextBody =
                $"Hi {dto.Username},\n\nYou've been invited to join Dappi.\nTemporary password: {generatedPassword}\n\nAccept your invitation here:\n{acceptUrl}\n\nThis link expires in 48 hours.";
            var emailHtmlBody =
                $"<p>Hi {dto.Username},</p><p>You've been invited to join Dappi.</p><p><strong>Temporary password:</strong> {generatedPassword}</p><p><a href=\"{acceptUrl}\">Accept invitation</a></p><p>This link expires in 48 hours.</p>";

            if (_emailService is null)
            {
                return Ok(new
                {
                    message = "Invitation prepared, but email service is not configured.",
                });
            }

            var messageId = await _emailService.SendEmailAsync(
                [dto.Email],
                emailHtmlBody,
                emailTextBody,
                emailSubject
            );

            return Ok(new
            {
                message = "Invitation sent successfully.",
                emailSent = true,
                messageId,
                invitationLink = acceptUrl,
                frontendInvitationLink = frontendAcceptUrl,
                fallbackApiAcceptLink = acceptUrl
            });
        }

        [AllowAnonymous]
        [HttpGet("accept-invitation")]
        public async Task<IActionResult> AcceptInvitation([FromQuery] string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return BadRequest(new { message = "Invitation token is required." });
            }

            if (!TryGetInvitationPayload(token, out var invitation, out var tokenError))
            {
                return BadRequest(new { message = tokenError });
            }

            if (invitation.ExpiresAtUtc < DateTime.UtcNow)
            {
                return BadRequest(new { message = "Invitation token has expired." });
            }

            var existingUserByUsername = await _userManager.FindByNameAsync(invitation.Username);
            var existingUserByEmail = await _userManager.FindByEmailAsync(invitation.Email);

            if (existingUserByUsername is not null || existingUserByEmail is not null)
            {
                if (existingUserByUsername is null || existingUserByEmail is null)
                {
                    return BadRequest(new
                    {
                        message = "An invitation-related account already exists with conflicting data.",
                    });
                }

                if (
                    !string.Equals(
                        existingUserByUsername.Id,
                        existingUserByEmail.Id,
                        StringComparison.Ordinal
                    )
                )
                {
                    return BadRequest(new
                    {
                        message = "An invitation-related account already exists with conflicting data.",
                    });
                }
            }

            if (existingUserByUsername is null)
            {
                var user = new DappiUser
                {
                    UserName = invitation.Username,
                    Email = invitation.Email,
                    EmailConfirmed = false,
                };
                var result = await _userManager.CreateAsync(user, invitation.Password);

                if (!result.Succeeded)
                {
                    return BadRequest(new
                    {
                        message = result.Errors.FirstOrDefault()?.Description ?? "Failed to create user from invitation.",
                    });
                }

                var rolesToAssign = invitation.Roles.Count > 0 ? invitation.Roles : new List<string> { Constants.UserRoles.User };

                foreach (var role in rolesToAssign)
                {
                    var addRoleResult = await _userManager.AddToRoleAsync(user, role);

                    if (!addRoleResult.Succeeded)
                    {
                        return BadRequest(new
                        {
                            message = addRoleResult.Errors.FirstOrDefault()?.Description ?? "Failed to assign role to invited user.",
                        });
                    }
                }
            }

            var completeInvitationUrl = $"{Request.Scheme}://{Request.Host}/complete-invitation?token={token}";

            return Redirect(completeInvitationUrl);
        }

        [AllowAnonymous]
        [HttpPost("complete-invitation")]
        public async Task<IActionResult> CompleteInvitation([FromBody] CompleteInvitationDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Token))
            {
                return BadRequest(new { message = "Invitation token is required." });
            }

            if (string.IsNullOrWhiteSpace(dto.OldPassword) || string.IsNullOrWhiteSpace(dto.NewPassword))
            {
                return BadRequest(new { message = "Both old and new passwords are required." });
            }

            if (!TryGetInvitationPayload(dto.Token, out var invitation, out var tokenError))
            {
                return BadRequest(new { message = tokenError });
            }

            if (invitation.ExpiresAtUtc < DateTime.UtcNow)
            {
                return BadRequest(new { message = "Invitation token has expired." });
            }

            var user = await _userManager.FindByNameAsync(invitation.Username);
            if (user is null || !string.Equals(user.Email, invitation.Email, StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { message = "Invitation is not accepted yet." });
            }

            var passwordMatches = await _userManager.CheckPasswordAsync(user, dto.OldPassword);
            if (!passwordMatches)
            {
                return BadRequest(new { message = "Old password is incorrect." });
            }

            var changePasswordResult = await _userManager.ChangePasswordAsync(
                user,
                dto.OldPassword,
                dto.NewPassword
            );

            if (!changePasswordResult.Succeeded)
            {
                return BadRequest(new
                {
                    message = changePasswordResult.Errors.FirstOrDefault()?.Description ?? "Failed to change password.",
                });
            }

            if (!user.EmailConfirmed)
            {
                user.EmailConfirmed = true;
                var verifyResult = await _userManager.UpdateAsync(user);
                if (!verifyResult.Succeeded)
                {
                    return BadRequest(new
                    {
                        message = verifyResult.Errors.FirstOrDefault()?.Description ?? "Password changed, but failed to mark user as verified.",
                    });
                }
            }

            return Ok(new { message = "Password changed successfully. User verified." });
        }

        [HttpGet]
        public IActionResult GetUsers([FromQuery] int offset = 0, [FromQuery] int limit = 10,
            [FromQuery] string searchTerm = "")
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
                    Id = user.Id, Name = user.UserName, Email = user.Email, Roles = roles.ToList()
                });
            }

            var response = new PagedResponseDto<UserRoleDto>
            {
                Total = totalCount, Offset = offset, Limit = limit, Data = result
            };

            _logger.LogInformation("Retrieved {Count} users out of {Total}", result.Count, totalCount);
            return Ok(response);
        }

        [HttpGet("username/{username}")]
        public async Task<IActionResult> GetUserByUsername(string username)
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
                Id = user.Id, Name = user.UserName, Email = user.Email, Roles = roles.ToList()
            };

            _logger.LogInformation("Retrieved user {Username}", username);
            return Ok(userDto);
        }

        [HttpPut("{username}/roles")]
        public async Task<IActionResult> UpdateUserRoles(string username, [FromBody] UserRolesUpdateDto dto)
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
            return Ok(new { message = "User roles updated successfully", roles = updatedRoles });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(string id, [FromBody] UserDto dto)
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
            return Ok(new { id = user.Id, name = user.UserName, email = user.Email, roles = updatedRoles });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(string id)
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

        private bool TryGetInvitationPayload(
            string token,
            out InvitationPayload invitation,
            out string error
        )
        {
            try
            {
                var decodedTokenBytes = WebEncoders.Base64UrlDecode(token);
                var protectedToken = Encoding.UTF8.GetString(decodedTokenBytes);
                var unprotectedPayload = _invitationProtector.Unprotect(protectedToken);
                var parsedInvitation = JsonSerializer.Deserialize<InvitationPayload>(unprotectedPayload);

                if (parsedInvitation is null)
                {
                    invitation = default!;
                    error = "Invitation payload is invalid.";
                    return false;
                }

                invitation = parsedInvitation;
                error = string.Empty;
                return true;
            }
            catch (Exception)
            {
                invitation = default!;
                error = "Invitation token is invalid.";
                return false;
            }
        }

        private sealed record InvitationPayload(
            string Username,
            string Email,
            string Password,
            List<string> Roles,
            DateTime ExpiresAtUtc
        );

        private static string GeneratePassword()
        {
            const string lower = "abcdefghijklmnopqrstuvwxyz";
            const string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string digits = "0123456789";
            const string symbols = "!@#$%^&*()_-+=[]{}<>?";

            var requiredCharacters = new List<char>
            {
                GetRandomCharacter(lower),
                GetRandomCharacter(upper),
                GetRandomCharacter(digits),
                GetRandomCharacter(symbols),
            };

            var allCharacters = lower + upper + digits + symbols;
            const int totalLength = 12;

            while (requiredCharacters.Count < totalLength)
            {
                requiredCharacters.Add(GetRandomCharacter(allCharacters));
            }

            for (var index = requiredCharacters.Count - 1; index > 0; index--)
            {
                var swapIndex = RandomNumberGenerator.GetInt32(index + 1);
                (requiredCharacters[index], requiredCharacters[swapIndex]) =
                    (requiredCharacters[swapIndex], requiredCharacters[index]);
            }

            return new string(requiredCharacters.ToArray());
        }

        private static char GetRandomCharacter(string source)
        {
            var position = RandomNumberGenerator.GetInt32(source.Length);
            return source[position];
        }
    }
}
