using Dappi.HeadlessCms.Database;
using Microsoft.EntityFrameworkCore;
using MyCompany.MyProject.WebApi.Entities;

namespace MyCompany.MyProject.WebApi.Data;
public class AppDbContext : DappiDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Book> Books { get; set; }
    public DbSet<Author> Authors { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Author>().HasMany<Book>(s => s.Books).WithOne(e => e.Author).HasForeignKey(s => s.AuthorId);
        base.OnModelCreating(modelBuilder);
    }
}