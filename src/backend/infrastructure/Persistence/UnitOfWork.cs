using ComiCal.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ComiCal.Infrastructure.Persistence;

/// <summary>
/// EF Core 実装の <see cref="IUnitOfWork"/>。
/// 既存トランザクション中に呼ばれた場合はネストせず、現在のスコープを共有する。
/// </summary>
public sealed class UnitOfWork(ComiCalDbContext db) : IUnitOfWork
{
    private readonly ComiCalDbContext _db = db ?? throw new ArgumentNullException(nameof(db));

    /// <inheritdoc />
    public async Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (_db.Database.CurrentTransaction is not null)
        {
            return await action(cancellationToken).ConfigureAwait(false);
        }

        await using var tx = await _db.Database
            .BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var value = await action(cancellationToken).ConfigureAwait(false);
            await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
            return value;
        }
        catch
        {
            await tx.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            throw;
        }
    }
}
