using MyCompany.MyProject.WebApi.Entities;
using CCApi.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace MyCompany.MyProject.WebApi.Data;

public class AppDbContext : IdentityDbContext<DappiUser, DappiRole, string>
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

	public DbSet<testCollection> testCollections { get; set; }

	public DbSet<test1> test1s { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<test1>()
            .HasMany(m => m.testCollections)
            .WithMany(r => r.test1s)
            .UsingEntity(j => j.ToTable("test1testCollections"));

        base.OnModelCreating(modelBuilder);
    }}