using ComiCal.Domain.Entities;

namespace ComiCal.Domain.Repositories;

public interface IUserRepository
{
    Task<User?> FindByIdAsync(Guid userId, CancellationToken ct = default);
    Task<User?> FindByIdentityAsync(string provider, string subject, CancellationToken ct = default);
    Task<Guid> UpsertAsync(User user, CancellationToken ct = default);
    Task SoftDeleteAsync(Guid userId, CancellationToken ct = default);
}
