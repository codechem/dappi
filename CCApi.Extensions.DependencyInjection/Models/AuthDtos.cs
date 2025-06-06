namespace CCApi.Extensions.DependencyInjection.Models
{
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

    public class UserRoleUpdateDto
    {
        public string Role { get; set; }
    }

    public class UserRolesUpdateDto
    {
        public List<string> Roles { get; set; }
    }
}