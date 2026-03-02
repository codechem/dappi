using Dappi.HeadlessCms.UsersAndPermissions.Core;
using Microsoft.EntityFrameworkCore;

namespace Dappi.HeadlessCms.UsersAndPermissions.Database
{
    public abstract class UsersAndPermissionsDbContext(DbContextOptions options)
        : DbContext(options)
    {
        public DbSet<AppPermission> AppPermissions { get; set; }
        public DbSet<AppRole> AppRoles { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("UsersAndPermissions");

            modelBuilder.Entity<AppPermission>(entity =>
            {
                entity.HasKey(p => p.Id);
                entity.Property(p => p.Name).IsRequired();
                entity.Property(p => p.Description).IsRequired();
            });
            modelBuilder.Entity<AppRole>(entity =>
            {
                entity.HasKey(p => p.Id);
                entity.Property(p => p.Name).IsRequired();
                entity.HasMany(r => r.Permissions);
            });

            modelBuilder.Entity<RefreshToken>(e =>
            {
                e.HasIndex(t => t.Token).IsUnique();
                e.HasIndex(t => t.UserId);
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}
