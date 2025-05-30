using Dappi.HeadlessCms.Database;
using Dappi.HeadlessCms.Interfaces;

namespace Dappi.HeadlessCms.Services
{
    public class DbContextAccessor<TDbContext> : IDbContextAccessor where TDbContext : DappiDbContext
    {
        private readonly TDbContext _dbContext;

        public DbContextAccessor(TDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public DappiDbContext DbContext => _dbContext;
    }
}