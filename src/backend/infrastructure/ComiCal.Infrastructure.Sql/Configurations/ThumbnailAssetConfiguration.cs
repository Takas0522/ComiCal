using ComiCal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ComiCal.Infrastructure.Sql.Configurations;

public sealed class ThumbnailAssetConfiguration : IEntityTypeConfiguration<ThumbnailAsset>
{
    public void Configure(EntityTypeBuilder<ThumbnailAsset> builder)
    {
        builder.ToTable("ThumbnailAssets");
        builder.HasKey(ta => ta.VolumeId);
        builder.Property(ta => ta.BlobKey).HasMaxLength(256).IsRequired();
        builder.Property(ta => ta.ContentHash).HasColumnType("binary(32)").IsRequired();
        builder.Property(ta => ta.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(ta => ta.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
    }
}
