namespace Dappi.HeadlessCms.UsersAndPermissions.Database
{
    public class DbContextAccessor<TDbContext> : IDbContextAccessor
        where TDbContext : UsersAndPermissionsDbContext
    {
        private readonly TDbContext _dbContext;

        public DbContextAccessor(TDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public UsersAndPermissionsDbContext DbContext => _dbContext;
    }
}
