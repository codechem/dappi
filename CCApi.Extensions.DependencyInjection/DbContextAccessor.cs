using Microsoft.EntityFrameworkCore;

namespace CCApi.Extensions.DependencyInjection;

public interface IDbContextAccessor
{
    DbContext DbContext { get; }
}

/// <summary>
/// Used to access the registered application's DbContext instance through the abstract one,
/// so we can perform basic database operations. 
/// </summary>
/// <typeparam name="TDbContext"></typeparam>
public class DbContextAccessor<TDbContext> : IDbContextAccessor
    where TDbContext : DbContext
{
    private readonly TDbContext _dbContext;

    public DbContextAccessor(TDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public DbContext DbContext => _dbContext;
}