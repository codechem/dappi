using CCApi.WebApiExample.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CCApi.WebApiExample.Data;

public partial class AppDbContext : IdentityDbContext<DappiUser, DappiRole, string>
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    public DbSet<Book> Books { get; set; }
    public DbSet<Movie> Movies { get; set; }
    public DbSet<Author> Authors { get; set; }
    public DbSet<Motorcycle> Motorcycles { get; set; }
}
