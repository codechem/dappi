using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CCApi.Extensions.DependencyInjection.Interfaces;

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