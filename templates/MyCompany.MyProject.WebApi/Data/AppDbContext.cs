using MyCompany.MyProject.WebApi.Entities;
using CCApi.Extensions.DependencyInjection.Database;
using Microsoft.EntityFrameworkCore;

namespace MyCompany.MyProject.WebApi.Data;

public class AppDbContext : DappiDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }
	public DbSet<NewCollection> NewCollections { get; set; }

	public DbSet<NewestCollection> NewestCollections { get; set; }

	public DbSet<NewNewCollection> NewNewCollections { get; set; }

	public DbSet<NewCollectionTest> NewCollectionTests { get; set; }
}
