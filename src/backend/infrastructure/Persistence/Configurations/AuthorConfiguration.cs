using ComiCal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ComiCal.Infrastructure.Persistence.Configurations;

internal sealed class AuthorConfiguration : IEntityTypeConfiguration<Author>
{
    public void Configure(EntityTypeBuilder<Author> builder)
    {
        builder.ToTable("Authors", "dbo");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id)
            .HasColumnName("AuthorId")
            .ValueGeneratedNever();

        builder.Property(a => a.Name)
            .HasColumnType("nvarchar(128)")
            .IsRequired();

        builder.Property(a => a.NormalizedName)
            .HasColumnType("nvarchar(128)")
            .IsRequired();

        builder.Property(a => a.NormalizedNameHiragana)
            .HasColumnType("nvarchar(128)")
            .HasComputedColumnSql("[dbo].[fnToHiragana]([NormalizedName])", stored: true)
            .ValueGeneratedOnAddOrUpdate();

        builder.Property(a => a.IsDeleted);
        builder.Property(a => a.DeletedAt).HasColumnType("datetime2(0)");
        builder.Property(a => a.CreatedAt).HasColumnType("datetime2(0)");
        builder.Property(a => a.UpdatedAt).HasColumnType("datetime2(0)");

        builder.HasIndex(a => a.NormalizedName).IsUnique();
        builder.HasQueryFilter(a => !a.IsDeleted);
    }
}
