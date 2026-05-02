using ComiCal.Domain.Entities;
using ComiCal.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ComiCal.Infrastructure.Sql.Repositories;

public sealed class AuthorRepository(ComiCalDbContext db) : IAuthorRepository
{
    public Task<Author?> FindByNormalizedNameAsync(string normalizedName, CancellationToken ct = default)
        => db.Authors
            .Where(a => a.NormalizedName == normalizedName)
            .FirstOrDefaultAsync(ct);

    public async Task<Guid> UpsertAsync(Author author, CancellationToken ct = default)
    {
        var existing = await db.Authors.FindAsync([author.AuthorId], ct);
        if (existing is null)
            db.Authors.Add(author);
        else
            db.Entry(existing).CurrentValues.SetValues(author);
        await db.SaveChangesAsync(ct);
        return author.AuthorId;
    }
}
