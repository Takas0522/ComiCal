using ComiCal.Domain.Entities;
using ComiCal.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ComiCal.Infrastructure.Persistence.Configurations;

internal sealed class VolumeConfiguration : IEntityTypeConfiguration<Volume>
{
    public void Configure(EntityTypeBuilder<Volume> builder)
    {
        builder.ToTable("Volumes", "dbo");
        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id)
            .HasColumnName("VolumeId")
            .ValueGeneratedNever();

        builder.Property(v => v.SeriesId)
            .HasColumnName("SeriesId");

        builder.Property(v => v.Isbn)
            .HasColumnName("Isbn13")
            .HasColumnType("char(13)")
            .HasMaxLength(13)
            .IsRequired()
            .HasConversion(
                v => v.Value,
                s => Isbn13.Create(s));

        builder.HasIndex(v => v.Isbn)
            .IsUnique()
            .HasDatabaseName("UQ_Volumes_Isbn13");

        builder.Property(v => v.VolumeNumber);
        builder.Property(v => v.ReleaseDate)
            .HasColumnType("date");
        builder.Property(v => v.ReleaseDateIsMonthOnly);

        builder.Property(v => v.CoverHash)
            .HasColumnName("CoverHash")
            .HasColumnType("binary(32)")
            .HasConversion(
                v => v.IsEmpty ? null : v.ToArray(),
                b => b == null ? ReadOnlyMemory<byte>.Empty : new ReadOnlyMemory<byte>(b));

        builder.Property(v => v.RakutenItemUrl)
            .HasColumnType("nvarchar(512)");

        builder.Property(v => v.IsDeleted);
        builder.Property(v => v.DeletedAt).HasColumnType("datetime2(0)");
        builder.Property(v => v.CreatedAt).HasColumnType("datetime2(0)");
        builder.Property(v => v.UpdatedAt).HasColumnType("datetime2(0)");

        builder.HasQueryFilter(v => !v.IsDeleted);
    }
}
