using CCApi.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace MyCompany.MyProject.WebApi.Data;

public class AppDbContext : IdentityDbContext<DappiUser, DappiRole, string>
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }
}