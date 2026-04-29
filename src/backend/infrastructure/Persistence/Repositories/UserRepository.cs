using ComiCal.Domain.Entities;
using ComiCal.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ComiCal.Infrastructure.Persistence.Repositories;

/// <summary>EF Core implementation of <see cref="IUserRepository"/>.</summary>
public sealed class UserRepository : IUserRepository
{
    private readonly ComiCalDbContext _db;
    private readonly ILogger<UserRepository> _logger;

    public UserRepository(ComiCalDbContext db, ILogger<UserRepository> logger)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(logger);
        _db = db;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<User?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(externalId);
        return await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.ExternalId == externalId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> HardDeleteAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
        {
            return false;
        }

        // Existence check first so we can return idempotent false without opening a tx.
        var exists = await _db.Users
            .IgnoreQueryFilters()
            .AsNoTracking()
            .AnyAsync(u => u.Id == userId, cancellationToken)
            .ConfigureAwait(false);
        if (!exists)
        {
            return false;
        }

        // FK children → parent. ExecuteSqlInterpolatedAsync uses parameterised SQL so the
        // GUID is bound, never inlined. Wrapped in a transaction so a partial delete cannot
        // leave dangling rows that violate FK constraints.
        await using var tx = await _db.Database
            .BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await _db.Database.ExecuteSqlInterpolatedAsync(
                $"DELETE FROM dbo.Purchases WHERE UserId = {userId}",
                cancellationToken).ConfigureAwait(false);
            await _db.Database.ExecuteSqlInterpolatedAsync(
                $"DELETE FROM dbo.Subscriptions WHERE UserId = {userId}",
                cancellationToken).ConfigureAwait(false);
            await _db.Database.ExecuteSqlInterpolatedAsync(
                $"DELETE FROM dbo.IdentityLinks WHERE UserId = {userId}",
                cancellationToken).ConfigureAwait(false);
            var rows = await _db.Database.ExecuteSqlInterpolatedAsync(
                $"DELETE FROM dbo.Users WHERE UserId = {userId}",
                cancellationToken).ConfigureAwait(false);

            await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogInformation(
                "Hard-deleted user {UserId} (rows affected on Users: {Rows}).",
                userId, rows);
            return rows > 0;
        }
        catch
        {
            await tx.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<User> EnsureExistsAsync(
        string externalId,
        string displayName,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(externalId);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        var existing = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.ExternalId == externalId, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var created = User.CreateNew(Guid.CreateVersion7(), externalId, displayName);
        _db.Users.Add(created);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            // Race: another concurrent invocation inserted the same ExternalId
            // between the SELECT and INSERT. Re-read and return that row.
            _logger.LogInformation(ex,
                "Concurrent insert detected for ExternalId {ExternalId}; re-reading existing row.",
                externalId);
            _db.Entry(created).State = EntityState.Detached;
            var raced = await _db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.ExternalId == externalId, cancellationToken);
            if (raced is null)
            {
                throw;
            }
            return raced;
        }

        return created;
    }
}
