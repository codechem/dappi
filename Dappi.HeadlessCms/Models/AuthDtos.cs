namespace Dappi.HeadlessCms.Models
{
    public class LoginDto
    {
        public required string Username { get; set; }
        public required string Password { get; set; }
    }

    public class RegisterDto
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class UserDto
    {
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new List<string>();
    }

    public class UserRoleDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new List<string>();
        public bool AcceptedInvitation { get; set; }
    }

    public class RoleDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int UserCount { get; set; }
    }

    public class CreateRoleDto
    {
        public string? Name { get; set; }
    }

    public class UserRoleUpdateDto
    {
        public string Role { get; set; } = string.Empty;
    }

    public class UserRolesUpdateDto
    {
        public List<string> Roles { get; set; } = new List<string>();
    }

    public class InviteUserDto
    {
        public required string Username { get; set; }
        public required string Email { get; set; }
        public List<string> Roles { get; set; } = [Constants.UserRoles.User];
    }

    public class CompleteInvitationDto
    {
        public required string Token { get; set; }
        public required string OldPassword { get; set; }
        public required string NewPassword { get; set; }
    }

    public class AcceptInvitationQueryDto
    {
        public string Token { get; set; } = string.Empty;
    }

    public sealed record InvitationPayload(
        string Username,
        string Email,
        string Password,
        List<string> Roles,
        DateTime ExpiresAtUtc
    );

    public sealed record InvitationPreparationResult(
        string Token,
        string GeneratedPassword,
        string AcceptUrl,
        string? FrontendAcceptUrl,
        string EmailSubject,
        string EmailTextBody,
        string EmailHtmlBody
    );
}