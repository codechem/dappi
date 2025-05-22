using CCApi.Extensions.DependencyInjection.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCApi.Extensions.DependencyInjection.Database.Configuration
{
    public class ContentTypeChangeConfiguration : IEntityTypeConfiguration<ContentTypeChange>
    {
        public void Configure(EntityTypeBuilder<ContentTypeChange> builder)
        {
            builder.Property(c => c.Id).UseIdentityColumn();

            builder.Property(c => c.ModelName).IsRequired()
                .HasMaxLength(256);

            builder
                .Property(c => c.Fields).HasColumnName("jsonb");
        }
    }
}