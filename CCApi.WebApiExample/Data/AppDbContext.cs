using CCApi.WebApiExample.Entities;
using Microsoft.EntityFrameworkCore;

namespace CCApi.WebApiExample.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Item> Items { get; set; }
    public DbSet<Book> Books { get; set; }
    public DbSet<Movie> Movies { get; set; }
    public DbSet<Author> Authors { get; set; }
}

public class Item
{
    public int Id { get; set; }
    public string Name { get; set; }
}

public class ItemChangeDto
{
    public string Name { get; set; }
}