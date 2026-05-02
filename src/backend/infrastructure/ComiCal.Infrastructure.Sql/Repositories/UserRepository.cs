using ComiCal.Domain.Entities;
using ComiCal.Domain.Enums;
using ComiCal.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ComiCal.Infrastructure.Sql.Repositories;

public sealed class UserRepository(ComiCalDbContext db) : IUserRepository
{
    public Task<User?> FindByIdAsync(Guid userId, CancellationToken ct = default)
        => db.Users.Include(u => u.IdentityLinks)
            .FirstOrDefaultAsync(u => u.UserId == userId, ct);

    public Task<User?> FindByIdentityAsync(string provider, string subject, CancellationToken ct = default)
    {
        if (!Enum.TryParse<IdentityProvider>(provider, ignoreCase: true, out var providerEnum))
            return Task.FromResult<User?>(null);

        return db.Users.Include(u => u.IdentityLinks)
            .FirstOrDefaultAsync(u => u.IdentityLinks.Any(il =>
                il.Provider == providerEnum && il.Subject == subject), ct);
    }

    public async Task<Guid> UpsertAsync(User user, CancellationToken ct = default)
    {
        var existing = await db.Users.FindAsync([user.UserId], ct);
        if (existing is null)
            db.Users.Add(user);
        else
            db.Entry(existing).CurrentValues.SetValues(user);
        await db.SaveChangesAsync(ct);
        return user.UserId;
    }

    public async Task SoftDeleteAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await db.Users.FindAsync([userId], ct);
        if (user is null) return;
        user.SoftDelete();
        await db.SaveChangesAsync(ct);
    }
}
