using ComiCal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ComiCal.Infrastructure.Sql.Configurations;

public sealed class VolumeConfiguration : IEntityTypeConfiguration<Volume>
{
    public void Configure(EntityTypeBuilder<Volume> builder)
    {
        builder.ToTable("Volumes");
        builder.HasKey(v => v.VolumeId);
        builder.Property(v => v.VolumeId).ValueGeneratedOnAdd();
        builder.Property(v => v.Isbn13).HasColumnType("char(13)").IsRequired();
        builder.HasIndex(v => v.Isbn13).IsUnique();
        builder.Property(v => v.ReleaseDate).HasColumnType("date");
        builder.Property(v => v.CoverHash).HasColumnType("binary(32)");
        builder.Property(v => v.RakutenItemUrl).HasMaxLength(512);
        builder.Property(v => v.IsDeleted).HasDefaultValue(false);
        builder.Property(v => v.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(v => v.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(v => v.ThumbnailAsset)
            .WithOne()
            .HasForeignKey<ThumbnailAsset>(ta => ta.VolumeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
