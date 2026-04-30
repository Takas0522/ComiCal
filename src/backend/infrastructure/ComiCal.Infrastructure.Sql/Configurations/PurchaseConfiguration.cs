using ComiCal.Domain.Entities;
using ComiCal.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ComiCal.Infrastructure.Sql.Configurations;

public sealed class PurchaseConfiguration : IEntityTypeConfiguration<Purchase>
{
    public void Configure(EntityTypeBuilder<Purchase> builder)
    {
        builder.ToTable("Purchases");
        builder.HasKey(p => p.PurchaseId);
        builder.Property(p => p.PurchaseId).ValueGeneratedOnAdd();
        builder.Property(p => p.State).HasMaxLength(20).HasConversion(
            v => v.ToString(),
            v => Enum.Parse<PurchaseState>(v));
        builder.HasIndex(p => new { p.UserId, p.VolumeId }).IsUnique();
        builder.Property(p => p.IsDeleted).HasDefaultValue(false);
        builder.Property(p => p.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(p => p.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(p => p.Volume)
            .WithMany()
            .HasForeignKey(p => p.VolumeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
