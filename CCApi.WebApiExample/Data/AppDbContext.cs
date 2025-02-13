using CCApi.WebApiExample.Entities;
using Microsoft.EntityFrameworkCore;

namespace CCApi.WebApiExample.Data;

public partial class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Book> Books { get; set; }
    public DbSet<Movie> Movies { get; set; }
    public DbSet<Author> Authors { get; set; }
}