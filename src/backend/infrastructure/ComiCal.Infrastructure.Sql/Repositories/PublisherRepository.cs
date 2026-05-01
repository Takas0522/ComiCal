using ComiCal.Domain.Entities;
using ComiCal.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ComiCal.Infrastructure.Sql.Repositories;

public sealed class PublisherRepository(ComiCalDbContext db) : IPublisherRepository
{
    public Task<Publisher?> FindByNormalizedNameAsync(string normalizedName, CancellationToken ct = default)
        => db.Publishers
            .Where(p => p.NormalizedName == normalizedName)
            .FirstOrDefaultAsync(ct);

    public async Task<Guid> UpsertAsync(Publisher publisher, CancellationToken ct = default)
    {
        var existing = await db.Publishers.FindAsync([publisher.PublisherId], ct);
        if (existing is null)
            db.Publishers.Add(publisher);
        else
            db.Entry(existing).CurrentValues.SetValues(publisher);
        await db.SaveChangesAsync(ct);
        return publisher.PublisherId;
    }
}
