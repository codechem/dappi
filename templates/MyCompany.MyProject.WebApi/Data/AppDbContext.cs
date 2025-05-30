using CCApi.Extensions.DependencyInjection.Database;
using Microsoft.EntityFrameworkCore;

namespace MyCompany.MyProject.WebApi.Data;

public class AppDbContext : DappiDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }
}