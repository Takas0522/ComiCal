using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ComiCal.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Sets <c>CreatedAt</c> / <c>UpdatedAt</c> to <see cref="DateTime.UtcNow"/> on
/// inserted / updated entities so we don't depend solely on the SQL trigger
/// (which still acts as a safety net at the DB layer).
/// </summary>
public sealed class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ApplyAudit(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ApplyAudit(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void ApplyAudit(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var now = DateTime.UtcNow;
        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Added)
            {
                TrySet(entry, "CreatedAt", now);
                TrySet(entry, "UpdatedAt", now);
            }
            else if (entry.State == EntityState.Modified)
            {
                TrySet(entry, "UpdatedAt", now);
            }
        }
    }

    private static void TrySet(EntityEntry entry, string propertyName, DateTime value)
    {
        var prop = entry.Metadata.FindProperty(propertyName);
        if (prop is null || prop.ClrType != typeof(DateTime))
        {
            return;
        }
        entry.Property(propertyName).CurrentValue = value;
    }
}
