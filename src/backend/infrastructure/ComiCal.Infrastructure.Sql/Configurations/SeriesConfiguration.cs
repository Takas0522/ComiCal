using ComiCal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ComiCal.Infrastructure.Sql.Configurations;

public sealed class SeriesConfiguration : IEntityTypeConfiguration<Series>
{
    public void Configure(EntityTypeBuilder<Series> builder)
    {
        builder.ToTable("Series");
        builder.HasKey(s => s.SeriesId);
        builder.Property(s => s.SeriesId).ValueGeneratedOnAdd();
        builder.Property(s => s.Title).HasMaxLength(512).IsRequired();
        builder.Property(s => s.NormalizedTitle).HasMaxLength(512).IsRequired();
        // NormalizedTitleHiragana is a computed PERSISTED column managed by the DACPAC schema.
        // We map it as a read-only shadow property so we can target it with EF.Functions.Contains
        // (the FT index lives on this column).
        builder.Property<string>("NormalizedTitleHiragana")
            .HasMaxLength(512)
            .HasComputedColumnSql("[dbo].[fnToHiragana]([NormalizedTitle])", stored: true)
            .ValueGeneratedOnAddOrUpdate()
            .Metadata.SetAfterSaveBehavior(Microsoft.EntityFrameworkCore.Metadata.PropertySaveBehavior.Ignore);
        builder.Property(s => s.IsCompleted).HasDefaultValue(false);
        builder.Property(s => s.IsDeleted).HasDefaultValue(false);
        builder.Property(s => s.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(s => s.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(s => s.Publisher)
            .WithMany()
            .HasForeignKey(s => s.PublisherId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<Author>()
            .WithMany()
            .HasForeignKey(s => s.PrimaryAuthorId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(s => s.SeriesAuthors)
            .WithOne()
            .HasForeignKey(sa => sa.SeriesId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(s => s.SeriesAuthors)
            .HasField("_seriesAuthors")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(s => s.Volumes)
            .WithOne()
            .HasForeignKey(v => v.SeriesId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(s => s.Volumes)
            .HasField("_volumes")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
