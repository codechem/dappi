using CCApi.Extensions.DependencyInjection.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CCApi.Extensions.DependencyInjection.Services
{
    public class DbContextAccessor<TDbContext> : IDbContextAccessor where TDbContext : DbContext
    {
        private readonly TDbContext _dbContext;

        public DbContextAccessor(TDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public DbContext DbContext => _dbContext;
    }
}