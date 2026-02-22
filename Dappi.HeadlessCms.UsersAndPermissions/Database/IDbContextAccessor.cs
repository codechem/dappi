namespace Dappi.HeadlessCms.UsersAndPermissions.Database
{
    public interface IDbContextAccessor
    {
        UsersAndPermissionsDbContext DbContext { get; }
    }
}
