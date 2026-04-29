using ComiCal.Domain.Entities;
using ComiCal.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ComiCal.Infrastructure.Persistence.Configurations;

internal sealed class SeriesAuthorConfiguration : IEntityTypeConfiguration<SeriesAuthor>
{
    public void Configure(EntityTypeBuilder<SeriesAuthor> builder)
    {
        builder.ToTable("SeriesAuthors", "dbo");
        builder.HasKey(sa => sa.Id);
        builder.Property(sa => sa.Id)
            .HasColumnName("SeriesAuthorId")
            .ValueGeneratedNever();

        builder.Property(sa => sa.SeriesId);
        builder.Property(sa => sa.AuthorId);

        builder.Property(sa => sa.Role)
            .HasColumnType("nvarchar(16)")
            .IsRequired()
            .HasConversion(
                r => RoleToString(r),
                s => RoleFromString(s));

        builder.Property<bool>("IsDeleted").HasDefaultValue(false);
        builder.HasQueryFilter(sa => !EF.Property<bool>(sa, "IsDeleted"));
    }

    private static string RoleToString(AuthorRole role) => role switch
    {
        AuthorRole.Primary => "Primary",
        AuthorRole.Co => "Co",
        AuthorRole.Original => "Original",
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Unknown AuthorRole."),
    };

    private static AuthorRole RoleFromString(string s) => s switch
    {
        "Primary" => AuthorRole.Primary,
        "Co" => AuthorRole.Co,
        "Original" => AuthorRole.Original,
        _ => throw new ArgumentOutOfRangeException(nameof(s), s, "Unknown AuthorRole string."),
    };
}
