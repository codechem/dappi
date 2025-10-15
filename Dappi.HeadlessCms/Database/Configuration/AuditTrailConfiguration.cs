using Dappi.HeadlessCms.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dappi.HeadlessCms.Database.Configuration
{
    public class AuditTrailConfiguration : IEntityTypeConfiguration<AuditTrail>
    {
        public void Configure(EntityTypeBuilder<AuditTrail> builder)
        {
            builder.HasKey(e => e.Id);

            builder.HasIndex(e => e.EntityId);
            builder.HasIndex(e => e.DateUtc).IsDescending(true);

            builder.Property(e => e.Id);

            builder.Property(e => e.UserId).HasMaxLength(500);
            builder.Property(e => e.EntityName).HasMaxLength(100).IsRequired();
            builder.Property(e => e.DateUtc).IsRequired();
            builder.Property(e => e.EntityId).HasMaxLength(100);

            builder.Property(e => e.TrailType).HasConversion<string>();

            builder.Property(e => e.ChangedColumns).HasColumnType("jsonb");
            builder.Property(e => e.OldValues).HasColumnType("jsonb");
            builder.Property(e => e.NewValues).HasColumnType("jsonb");
        }
    }

}