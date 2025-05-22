using CCApi.Extensions.DependencyInjection.Database;

namespace CCApi.Extensions.DependencyInjection.Interfaces
{
    public interface IDbContextAccessor
    {
        DappiDbContext DbContext { get; }
    }
}