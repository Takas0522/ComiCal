using ComiCal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ComiCal.Infrastructure.Persistence.Configurations;

internal sealed class SeriesConfiguration : IEntityTypeConfiguration<Series>
{
    public void Configure(EntityTypeBuilder<Series> builder)
    {
        builder.ToTable("Series", "dbo");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id)
            .HasColumnName("SeriesId")
            .ValueGeneratedNever();

        builder.Property(s => s.Title)
            .HasColumnType("nvarchar(256)")
            .IsRequired();

        builder.Property(s => s.NormalizedTitle)
            .HasColumnType("nvarchar(256)")
            .IsRequired();

        builder.Property(s => s.NormalizedTitleHiragana)
            .HasColumnType("nvarchar(256)")
            .HasComputedColumnSql("[dbo].[fnToHiragana]([NormalizedTitle])", stored: true)
            .ValueGeneratedOnAddOrUpdate();

        builder.Property(s => s.PublisherId);
        builder.Property(s => s.PrimaryAuthorId);
        builder.Property(s => s.IsCompleted);
        builder.Property(s => s.IsDeleted);
        builder.Property(s => s.DeletedAt).HasColumnType("datetime2(0)");
        builder.Property(s => s.CreatedAt).HasColumnType("datetime2(0)");
        builder.Property(s => s.UpdatedAt).HasColumnType("datetime2(0)");

        builder.HasMany(s => s.Volumes)
            .WithOne()
            .HasForeignKey(v => v.SeriesId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(s => s.Authors)
            .WithOne()
            .HasForeignKey(sa => sa.SeriesId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Metadata.FindNavigation(nameof(Series.Volumes))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(Series.Authors))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasQueryFilter(s => !s.IsDeleted);
    }
}
