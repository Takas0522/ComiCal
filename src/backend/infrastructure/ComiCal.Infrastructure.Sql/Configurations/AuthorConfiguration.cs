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
        builder.Property(a => a.Name).HasMaxLength(256).IsRequired();
        builder.Property(a => a.NormalizedName).HasMaxLength(256).IsRequired();
        builder.Property<string>("NormalizedNameHiragana")
            .HasMaxLength(256)
            .HasComputedColumnSql("[dbo].[fnToHiragana]([NormalizedName])", stored: true)
            .ValueGeneratedOnAddOrUpdate()
            .Metadata.SetAfterSaveBehavior(Microsoft.EntityFrameworkCore.Metadata.PropertySaveBehavior.Ignore);
        builder.Property(a => a.IsDeleted).HasDefaultValue(false);
        builder.Property(a => a.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(a => a.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
    }
}
