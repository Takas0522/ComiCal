using ComiCal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ComiCal.Infrastructure.Persistence.Configurations;

internal sealed class PublisherConfiguration : IEntityTypeConfiguration<Publisher>
{
    public void Configure(EntityTypeBuilder<Publisher> builder)
    {
        builder.ToTable("Publishers", "dbo");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id)
            .HasColumnName("PublisherId")
            .ValueGeneratedNever();

        builder.Property(p => p.Name)
            .HasColumnType("nvarchar(128)")
            .IsRequired();

        builder.Property(p => p.NormalizedName)
            .HasColumnType("nvarchar(128)")
            .IsRequired();

        builder.Property(p => p.NormalizedNameHiragana)
            .HasColumnType("nvarchar(128)")
            .HasComputedColumnSql("[dbo].[fnToHiragana]([NormalizedName])", stored: true)
            .ValueGeneratedOnAddOrUpdate();

        builder.Property(p => p.IsDeleted);
        builder.Property(p => p.DeletedAt).HasColumnType("datetime2(0)");
        builder.Property(p => p.CreatedAt).HasColumnType("datetime2(0)");
        builder.Property(p => p.UpdatedAt).HasColumnType("datetime2(0)");

        builder.HasIndex(p => p.NormalizedName).IsUnique();
        builder.HasQueryFilter(p => !p.IsDeleted);
    }
}
