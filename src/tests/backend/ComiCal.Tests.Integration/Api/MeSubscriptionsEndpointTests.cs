using ComiCal.Tests.Integration.Fixtures;
using FluentAssertions;
using Xunit;

namespace ComiCal.Tests.Integration.Api;

/// <summary>
/// Phase 2 placeholder for /api/me/subscriptions integration tests. Hosting
/// Functions Isolated Worker inside <c>WebApplicationFactory</c> is deferred to
/// Stage Z (same rationale as <see cref="SeriesEndpointTests"/>); the Application-
/// and Domain-layer behaviour is exhaustively covered by
/// <c>ComiCal.Tests.Unit.Application.Me.*</c>.
/// </summary>
[Collection(ApiIntegrationCollection.Name)]
[Trait("Category", "Integration")]
public sealed class MeSubscriptionsEndpointTests
{
    private readonly MsSqlFixture _mssql;

    public MeSubscriptionsEndpointTests(MsSqlFixture mssql)
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
        // TODO: POST /api/me/subscriptions -> 201, GET /api/me/subscriptions -> contains item,
        // DELETE /api/me/subscriptions/{seriesId} -> 204, GET -> empty.
    }

    [Fact(Skip = "Stage Z hardening: requires Testcontainers + DACPAC publish in CI.")]
    public void Anonymous_caller_gets_401_problem_json()
    {
        // TODO: assert 401 application/problem+json without x-ms-client-principal header.
    }
}
