using Microsoft.EntityFrameworkCore;

namespace CCApi.Extensions.DependencyInjection.Interfaces
{
    public interface IDbContextAccessor
    {
        DbContext DbContext { get; }
    }
}