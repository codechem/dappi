using CCApi.WebApiExample.Entities;
using Microsoft.EntityFrameworkCore;

namespace CCApi.WebApiExample.Data;

public partial class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Book> Books { get; set; }
    public DbSet<Movie> Movies { get; set; }
    public DbSet<Author> Authors { get; set; }
    public DbSet<Motorcycle> Motorcycles { get; set; }
}