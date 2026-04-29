using ComiCal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ComiCal.Infrastructure.Persistence.Configurations;

internal sealed class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.ToTable("Subscriptions", "dbo");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id)
            .HasColumnName("SubscriptionId")
            .ValueGeneratedNever();

        builder.Property(s => s.UserId).IsRequired();
        builder.Property(s => s.SeriesId).IsRequired();

        builder.Property(s => s.IsDeleted);
        builder.Property(s => s.DeletedAt).HasColumnType("datetime2(0)");
        builder.Property(s => s.CreatedAt).HasColumnType("datetime2(0)");
        builder.Property(s => s.UpdatedAt).HasColumnType("datetime2(0)");

        builder.HasIndex(s => new { s.UserId, s.SeriesId })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("UQ_Subscriptions_Active_UserId_SeriesId");

        builder.HasQueryFilter(s => !s.IsDeleted);
    }
}
