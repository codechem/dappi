using Dappi.HeadlessCms.Database.Configuration;
using Dappi.HeadlessCms.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Dappi.HeadlessCms.Database
{
    public class DappiDbContext(DbContextOptions options)
        : IdentityDbContext<DappiUser, DappiRole, string>(options)
    {
        public DbSet<ContentTypeChange> ContentTypeChanges { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            new ContentTypeChangeConfiguration().Configure(builder.Entity<ContentTypeChange>());

            base.OnModelCreating(builder);
        }
    }
}