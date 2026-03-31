namespace Dappi.HeadlessCms.UsersAndPermissions.Api;

public record AuthResult(string AccessToken, string RefreshToken);

public record RefreshTokenDto(string RefreshToken);

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

public class InviteUserDto
{
    public required string Username { get; set; }
    public required string Email { get; set; }
    public string? Password { get; set; }
    public List<string> Roles { get; set; } = [];
}

public class PluginUserDto
{
    public int Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool EmailConfirmed { get; set; }
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
}
