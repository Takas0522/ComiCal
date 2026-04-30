using ComiCal.Domain.Entities;
using ComiCal.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ComiCal.Infrastructure.Sql.Configurations;

public sealed class SeriesAuthorConfiguration : IEntityTypeConfiguration<SeriesAuthor>
{
    public void Configure(EntityTypeBuilder<SeriesAuthor> builder)
    {
        builder.ToTable("SeriesAuthors");
        builder.HasKey(sa => new { sa.SeriesId, sa.AuthorId });
        builder.Property(sa => sa.Role).HasMaxLength(16).HasConversion(
            v => v.ToString(),
            v => Enum.Parse<SeriesAuthorRole>(v));

        builder.HasOne(sa => sa.Author)
            .WithMany()
            .HasForeignKey(sa => sa.AuthorId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
