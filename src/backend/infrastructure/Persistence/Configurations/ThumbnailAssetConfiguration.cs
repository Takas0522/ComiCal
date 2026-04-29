using ComiCal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ComiCal.Infrastructure.Persistence.Configurations;

internal sealed class ThumbnailAssetConfiguration : IEntityTypeConfiguration<ThumbnailAsset>
{
    public void Configure(EntityTypeBuilder<ThumbnailAsset> builder)
    {
        builder.ToTable("ThumbnailAssets", "dbo");
        builder.HasKey(t => t.VolumeId);
        builder.Property(t => t.VolumeId).ValueGeneratedNever();

        builder.Property(t => t.BlobKey)
            .HasColumnType("nvarchar(512)")
            .IsRequired();

        builder.Property(t => t.SizeBytes);
        builder.Property(t => t.Width);
        builder.Property(t => t.Height);

        builder.Property(t => t.ContentHash)
            .HasColumnType("binary(32)")
            .IsRequired()
            .HasConversion(
                v => v.ToArray(),
                b => new ReadOnlyMemory<byte>(b ?? Array.Empty<byte>()));

        builder.Property(t => t.IsDeleted);
        builder.Property(t => t.DeletedAt).HasColumnType("datetime2(0)");
        builder.Property(t => t.CreatedAt).HasColumnType("datetime2(0)");
        builder.Property(t => t.UpdatedAt).HasColumnType("datetime2(0)");

        builder.HasOne<Volume>()
            .WithOne()
            .HasForeignKey<ThumbnailAsset>(t => t.VolumeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(t => !t.IsDeleted);
    }
}
