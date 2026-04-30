using ComiCal.Domain.Entities;
using ComiCal.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ComiCal.Infrastructure.Sql.Configurations;

public sealed class BatchRunConfiguration : IEntityTypeConfiguration<BatchRun>
{
    public void Configure(EntityTypeBuilder<BatchRun> builder)
    {
        builder.ToTable("BatchRuns");
        builder.HasKey(b => b.BatchRunId);
        builder.Property(b => b.BatchRunId).ValueGeneratedOnAdd();
        builder.Property(b => b.Status).HasMaxLength(32).HasConversion(
            v => v.ToString(),
            v => Enum.Parse<BatchRunStatus>(v));

        // CreatedAt is in the DB schema but not on the entity — use shadow property
        builder.Property<DateTime>("CreatedAt").HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasMany(b => b.FailedItems)
            .WithOne()
            .HasForeignKey(fi => fi.BatchRunId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(b => b.FailedItems)
            .HasField("_failedItems")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
