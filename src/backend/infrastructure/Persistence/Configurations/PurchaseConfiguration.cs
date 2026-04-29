using ComiCal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ComiCal.Infrastructure.Persistence.Configurations;

internal sealed class PurchaseConfiguration : IEntityTypeConfiguration<Purchase>
{
    public void Configure(EntityTypeBuilder<Purchase> builder)
    {
        builder.ToTable("Purchases", "dbo");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id)
            .HasColumnName("PurchaseId")
            .ValueGeneratedNever();

        builder.Property(p => p.UserId).IsRequired();
        builder.Property(p => p.VolumeId).IsRequired();

        builder.Property(p => p.State)
            .HasColumnType("nvarchar(16)")
            .IsRequired();

        builder.Property(p => p.PurchasedAt).HasColumnType("datetime2(0)");

        builder.Property(p => p.IsDeleted);
        builder.Property(p => p.DeletedAt).HasColumnType("datetime2(0)");
        builder.Property(p => p.CreatedAt).HasColumnType("datetime2(0)");
        builder.Property(p => p.UpdatedAt).HasColumnType("datetime2(0)");

        builder.HasIndex(p => new { p.UserId, p.VolumeId })
            .IsUnique()
            .HasDatabaseName("UQ_Purchases_UserId_VolumeId");

        builder.HasQueryFilter(p => !p.IsDeleted);
    }
}
