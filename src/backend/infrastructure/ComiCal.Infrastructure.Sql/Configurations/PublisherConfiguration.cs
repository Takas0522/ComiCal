using ComiCal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ComiCal.Infrastructure.Sql.Configurations;

public sealed class PublisherConfiguration : IEntityTypeConfiguration<Publisher>
{
    public void Configure(EntityTypeBuilder<Publisher> builder)
    {
        builder.ToTable("Publishers");
        builder.HasKey(p => p.PublisherId);
        builder.Property(p => p.PublisherId).ValueGeneratedOnAdd();
        builder.Property(p => p.Name).HasMaxLength(128).IsRequired();
        builder.Property(p => p.NormalizedName).HasMaxLength(128).IsRequired();
        builder.Property(p => p.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(p => p.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
    }
}
