using FluentValidation;

namespace Dappi.HeadlessCms.UsersAndPermissions.Api;

public record AuthResult(string AccessToken, string RefreshToken);

public class RolePermissionDto
{
    public string PermissionName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool Selected { get; set; }
}

public class RegisterUserDto
{
    public required string Email { get; set; }
    public required string Password { get; set; }
}
