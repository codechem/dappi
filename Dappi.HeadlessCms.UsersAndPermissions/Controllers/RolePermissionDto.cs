namespace Dappi.HeadlessCms.UsersAndPermissions.Controllers
{
    public class RolePermissionDto
    {
        public string PermissionName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool Selected { get; set; }
    }
}
