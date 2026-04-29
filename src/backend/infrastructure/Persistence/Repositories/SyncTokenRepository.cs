using ComiCal.Domain.Entities;
using ComiCal.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ComiCal.Infrastructure.Persistence.Repositories;

/// <summary>EF Core implementation of <see cref="ISyncTokenRepository"/>.</summary>
public sealed class SyncTokenRepository(
    ComiCalDbContext db,
    ILogger<SyncTokenRepository> logger) : ISyncTokenRepository
{
    private readonly ComiCalDbContext _db = db ?? throw new ArgumentNullException(nameof(db));
    private readonly ILogger<SyncTokenRepository> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public async Task AddAsync(SyncToken token, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(token);
        _db.SyncTokens.Add(token);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Issued SyncToken {SyncTokenId} for user {UserId} (expires {ExpiresAt:o})",
            token.Id, token.UserId, token.ExpiresAt);
    }

    /// <inheritdoc />
    public async Task<SyncToken?> GetActiveByHashAsync(byte[] tokenHash, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(tokenHash);
        var nowUtc = DateTime.UtcNow;
        return await _db.SyncTokens
            .FirstOrDefaultAsync(
                t => t.TokenHash == tokenHash && t.ConsumedAt == null && t.ExpiresAt > nowUtc,
                cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<SyncToken?> FindByHashAsync(byte[] tokenHash, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(tokenHash);
        return await _db.SyncTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> MarkConsumedAsync(Guid syncTokenId, CancellationToken cancellationToken)
    {
        var entity = await _db.SyncTokens
            .FirstOrDefaultAsync(t => t.Id == syncTokenId, cancellationToken)
            .ConfigureAwait(false);
        if (entity is null || entity.ConsumedAt is not null)
        {
            return false;
        }
        entity.MarkConsumed(DateTime.UtcNow);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }
}
