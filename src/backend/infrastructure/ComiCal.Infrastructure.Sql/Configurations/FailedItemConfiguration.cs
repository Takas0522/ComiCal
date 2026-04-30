using ComiCal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ComiCal.Infrastructure.Sql.Configurations;

public sealed class FailedItemConfiguration : IEntityTypeConfiguration<FailedItem>
{
    public void Configure(EntityTypeBuilder<FailedItem> builder)
    {
        builder.ToTable("FailedItems");
        builder.HasKey(fi => fi.FailedItemId);
        builder.Property(fi => fi.FailedItemId).ValueGeneratedOnAdd();
        builder.Property(fi => fi.ItemKey).HasMaxLength(256).IsRequired();
        builder.Property(fi => fi.Reason).HasMaxLength(1024).IsRequired();
        builder.Property(fi => fi.PayloadJson).HasMaxLength(4000);
        builder.Property(fi => fi.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
    }
}
