using CCApi.Extensions.DependencyInjection.Database;
using CCApi.Extensions.DependencyInjection.Interfaces;

namespace CCApi.Extensions.DependencyInjection.Services
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