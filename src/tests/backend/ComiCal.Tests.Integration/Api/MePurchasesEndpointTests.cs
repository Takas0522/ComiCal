using ComiCal.Tests.Integration.Fixtures;
using FluentAssertions;
using Xunit;

namespace ComiCal.Tests.Integration.Api;

/// <summary>
/// Phase 2 placeholder for /api/me/purchases integration tests. Same Stage Z
/// deferral rationale as <see cref="MeSubscriptionsEndpointTests"/>.
/// </summary>
[Collection(ApiIntegrationCollection.Name)]
[Trait("Category", "Integration")]
public sealed class MePurchasesEndpointTests
{
    private readonly MsSqlFixture _mssql;

    public MePurchasesEndpointTests(MsSqlFixture mssql)
    {
        _mssql = mssql;
    }

    [Fact]
    public void Fixture_is_available()
    {
        _mssql.ConnectionString.Should().NotBeNullOrWhiteSpace();
    }

    [Fact(Skip = "Stage Z hardening: requires Testcontainers + DACPAC publish in CI.")]
    public void Add_then_list_then_delete_round_trip()
    {
        // TODO: POST /api/me/purchases -> 201, GET -> contains item, DELETE -> 204, GET -> empty.
    }

    [Fact(Skip = "Stage Z hardening: requires Testcontainers + DACPAC publish in CI.")]
    public void Add_is_idempotent_returns_200_on_second_call()
    {
        // TODO: second POST for same (UserId, VolumeId) returns 200 (not 201).
    }
}
