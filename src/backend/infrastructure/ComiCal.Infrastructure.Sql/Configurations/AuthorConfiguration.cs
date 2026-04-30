using ComiCal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ComiCal.Infrastructure.Sql.Configurations;

public sealed class AuthorConfiguration : IEntityTypeConfiguration<Author>
{
    public void Configure(EntityTypeBuilder<Author> builder)
    {
        builder.ToTable("Authors");
        builder.HasKey(a => a.AuthorId);
        builder.Property(a => a.AuthorId).ValueGeneratedOnAdd();
        builder.Property(a => a.Name).HasMaxLength(128).IsRequired();
        builder.Property(a => a.NormalizedName).HasMaxLength(128).IsRequired();
        // NormalizedNameHiragana is a computed PERSISTED column — not mapped to entity
        builder.Property(a => a.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(a => a.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
    }
}
