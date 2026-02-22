namespace Dappi.HeadlessCms.UsersAndPermissions.Core
{
    public interface IAppUser
    {
        int RoleId { get; }
        AppRole? Role { get; }
    }
}
