using ComiCal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ComiCal.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users", "dbo");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id)
            .HasColumnName("UserId")
            .ValueGeneratedNever();

        builder.Property(u => u.ExternalId)
            .HasColumnType("nvarchar(128)")
            .IsRequired();

        builder.Property(u => u.DisplayName)
            .HasColumnType("nvarchar(64)")
            .IsRequired();

        builder.Property(u => u.Role)
            .HasColumnType("nvarchar(16)")
            .IsRequired();

        builder.Property(u => u.IsDeleted);
        builder.Property(u => u.DeletedAt).HasColumnType("datetime2(0)");
        builder.Property(u => u.CreatedAt).HasColumnType("datetime2(0)");
        builder.Property(u => u.UpdatedAt).HasColumnType("datetime2(0)");

        builder.HasIndex(u => u.ExternalId).IsUnique();
        builder.HasQueryFilter(u => !u.IsDeleted);
    }
}
