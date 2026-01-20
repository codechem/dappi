using Dappi.HeadlessCms.Database;
using Microsoft.EntityFrameworkCore;
using MyCompany.MyProject.WebApi.Entities;

namespace MyCompany.MyProject.WebApi.Data;
public class AppDbContext : DappiDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Hehe> Hehes { get; set; }
}