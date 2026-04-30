using ComiCal.Domain.Entities;
using ComiCal.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ComiCal.Infrastructure.Sql.Configurations;

public sealed class IdentityLinkConfiguration : IEntityTypeConfiguration<IdentityLink>
{
    public void Configure(EntityTypeBuilder<IdentityLink> builder)
    {
        builder.ToTable("IdentityLinks");
        builder.HasKey(il => il.IdentityLinkId);
        builder.Property(il => il.IdentityLinkId).ValueGeneratedOnAdd();
        builder.Property(il => il.Provider).HasMaxLength(32).HasConversion(
            v => v.ToString().ToLowerInvariant(),
            v => Enum.Parse<IdentityProvider>(v, ignoreCase: true));
        builder.Property(il => il.Subject).HasMaxLength(256).IsRequired();
        builder.HasIndex(il => new { il.Provider, il.Subject }).IsUnique();
        builder.Property(il => il.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
    }
}
