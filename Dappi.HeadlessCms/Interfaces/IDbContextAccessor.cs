using Dappi.HeadlessCms.Database;

namespace Dappi.HeadlessCms.Interfaces
{
    public interface IDbContextAccessor
    {
        DappiDbContext DbContext { get; }
    }
}