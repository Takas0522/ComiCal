using ComiCal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ComiCal.Infrastructure.Persistence.Configurations;

internal sealed class SyncTokenConfiguration : IEntityTypeConfiguration<SyncToken>
{
    public void Configure(EntityTypeBuilder<SyncToken> builder)
    {
        builder.ToTable("SyncTokens", "dbo");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id)
            .HasColumnName("SyncTokenId")
            .ValueGeneratedNever();

        builder.Property(t => t.UserId).IsRequired();
        builder.Property(t => t.TokenHash)
            .HasColumnType("varbinary(32)")
            .IsRequired();

        builder.Property(t => t.ExpiresAt).HasColumnType("datetime2(0)");
        builder.Property(t => t.ConsumedAt).HasColumnType("datetime2(0)");
        builder.Property(t => t.CreatedAt).HasColumnType("datetime2(0)");

        builder.HasIndex(t => t.TokenHash)
            .IsUnique()
            .HasDatabaseName("UQ_SyncTokens_TokenHash");

        builder.HasIndex(t => new { t.UserId, t.ExpiresAt })
            .HasDatabaseName("IX_SyncTokens_UserId_ExpiresAt");
    }
}
