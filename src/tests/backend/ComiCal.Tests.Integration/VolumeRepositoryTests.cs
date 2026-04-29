using ComiCal.Domain.Entities;
using ComiCal.Domain.ValueObjects;
using ComiCal.Infrastructure.Persistence;
using ComiCal.Infrastructure.Persistence.Repositories;
using ComiCal.Tests.Integration.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ComiCal.Tests.Integration;

[Trait("Category", "Integration")]
public sealed class VolumeRepositoryTests
{
    [Fact(Skip = "Stage Z hardening: requires Testcontainers + DACPAC publish in CI.")]
    public async Task Upsert_then_GetByIsbn_round_trip()
    {
        await using var db = new ComiCalDbContext(
            new DbContextOptionsBuilder<ComiCalDbContext>()
                .UseSqlServer("Server=(local);Database=ComiCal;Integrated Security=true;TrustServerCertificate=true;")
                .Options);

        var repo = new VolumeRepository(db, NullLogger<VolumeRepository>.Instance);

        var seriesId = Guid.CreateVersion7();
        var isbn = Isbn13.Create("9784088100005");
        var vol = Volume.Create(seriesId, isbn, 100, new DateOnly(2026, 4, 3), false, ReadOnlyMemory<byte>.Empty, null);

        await repo.UpsertAsync(vol, TestContext.Current.CancellationToken);
        var fetched = await repo.GetByIsbnAsync(isbn, TestContext.Current.CancellationToken);

        fetched.Should().NotBeNull();
        fetched!.Isbn.Value.Should().Be(isbn.Value);
    }
}
