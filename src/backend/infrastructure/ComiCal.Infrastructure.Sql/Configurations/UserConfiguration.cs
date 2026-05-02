using ComiCal.Domain.Entities;
using ComiCal.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ComiCal.Infrastructure.Sql.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.UserId);
        builder.Property(u => u.UserId).ValueGeneratedOnAdd();
        builder.Property(u => u.DisplayName).HasMaxLength(64).IsRequired();
        builder.Property(u => u.Role).HasMaxLength(16).HasConversion(
            v => v.ToString(),
            v => Enum.Parse<UserRole>(v));
        builder.Property(u => u.IsDeleted).HasDefaultValue(false);
        builder.Property(u => u.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(u => u.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        // AgreedAt is a domain-layer concept not yet in the DB schema
        builder.Ignore(u => u.AgreedAt);

        builder.HasMany(u => u.IdentityLinks)
            .WithOne()
            .HasForeignKey(il => il.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(u => u.IdentityLinks)
            .HasField("_identityLinks")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
