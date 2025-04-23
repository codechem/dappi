using MyCompany.MyProject.WebApi.Entities;
using Microsoft.EntityFrameworkCore;

namespace MyCompany.MyProject.WebApi.Data;

public class AppDbContext : DbContext
{
	public DbSet<TestName> TestNames { get; set; }
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }
}