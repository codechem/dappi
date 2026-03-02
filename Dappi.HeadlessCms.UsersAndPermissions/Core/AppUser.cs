using Microsoft.AspNetCore.Identity;

namespace Dappi.HeadlessCms.UsersAndPermissions.Core
{
    public abstract class AppUser : IdentityUser<int>
    {
        public int RoleId { get; set; }
        public AppRole? Role { get; set; }
    }
}
