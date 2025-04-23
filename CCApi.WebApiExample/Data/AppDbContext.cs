using CCApi.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CCApi.WebApiExample.Data;

public partial class AppDbContext : IdentityDbContext<DappiUser, DappiRole, string>
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

}
