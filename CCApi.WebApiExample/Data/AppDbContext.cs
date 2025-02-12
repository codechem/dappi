using CCApi.WebApiExample.Entities;
using Microsoft.EntityFrameworkCore;

namespace CCApi.WebApiExample.Data;

public class Item
{
    public int Id { get; set; }
    public string Name { get; set; }
}

public class ItemChangeDto
{
    public string Name { get; set; }

}

// since it can be partial, we can also automate this
public partial class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Item> Items { get; set; }
    public DbSet<Book> Books { get; set; }
    public DbSet<Movie> Movies { get; set; }
    public DbSet<Author> Authors { get; set; }
}