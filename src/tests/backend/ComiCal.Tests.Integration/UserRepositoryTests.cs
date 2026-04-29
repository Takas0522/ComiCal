using ComiCal.Domain.Entities;
using ComiCal.Infrastructure.Persistence;
using ComiCal.Infrastructure.Persistence.Repositories;
using ComiCal.Tests.Integration.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ComiCal.Tests.Integration;

[Trait("Category", "Integration")]
public sealed class UserRepositoryTests
{
    [Fact(Skip = "Stage Z hardening: requires Testcontainers + DACPAC publish in CI.")]
    public async Task EnsureExistsAsync_is_idempotent_by_external_id()
    {
        await using var db = new ComiCalDbContext(
            new DbContextOptionsBuilder<ComiCalDbContext>()
                .UseSqlServer("Server=(local);Database=ComiCal;Integrated Security=true;TrustServerCertificate=true;")
                .Options);

        var repo = new UserRepository(db, NullLogger<UserRepository>.Instance);
        var ct = TestContext.Current.CancellationToken;
        var externalId = $"test-ext-{Guid.NewGuid():N}";

        var first = await repo.EnsureExistsAsync(externalId, "Tester", ct);
        var second = await repo.EnsureExistsAsync(externalId, "Different Display Name", ct);
        var fetched = await repo.GetByExternalIdAsync(externalId, ct);

        first.Id.Should().NotBe(Guid.Empty);
        second.Id.Should().Be(first.Id, "EnsureExistsAsync must be idempotent by ExternalId");
        second.DisplayName.Should().Be("Tester", "existing rows are not overwritten by a second call");
        fetched.Should().NotBeNull();
        fetched!.Id.Should().Be(first.Id);
        fetched.ExternalId.Should().Be(externalId);
    }

    [Fact(Skip = "Stage Z hardening: requires Testcontainers + DACPAC publish in CI.")]
    public async Task GetByExternalIdAsync_returns_null_for_unknown_external_id()
    {
        await using var db = new ComiCalDbContext(
            new DbContextOptionsBuilder<ComiCalDbContext>()
                .UseSqlServer("Server=(local);Database=ComiCal;Integrated Security=true;TrustServerCertificate=true;")
                .Options);

        var repo = new UserRepository(db, NullLogger<UserRepository>.Instance);
        var ct = TestContext.Current.CancellationToken;

        var result = await repo.GetByExternalIdAsync($"missing-{Guid.NewGuid():N}", ct);

        result.Should().BeNull();
    }
}
