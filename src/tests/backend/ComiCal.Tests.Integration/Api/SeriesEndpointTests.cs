using ComiCal.Tests.Integration.Fixtures;
using FluentAssertions;
using Xunit;

namespace ComiCal.Tests.Integration.Api;

/// <summary>
/// Phase 1 placeholder for Series API integration tests. Bootstrapping the
/// Functions Isolated Worker host inside <c>Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory</c>
/// is non-trivial (the worker uses its own host builder, not <c>WebApplication</c>)
/// and is therefore deferred to the Stage Z hardening pass — see
/// <c>HealthEndpointTests.Health_ReturnsOk</c> for the same approach.
/// The <see cref="ComiCal.Api.ProblemDetails.ProblemDetailsFactory"/> and ETag
/// computation paths are covered by <c>ComiCal.Tests.Unit.Api</c> in the meantime.
/// </summary>
[Collection(ApiIntegrationCollection.Name)]
[Trait("Category", "Integration")]
public sealed class SeriesEndpointTests
{
    private readonly MsSqlFixture _mssql;

    public SeriesEndpointTests(MsSqlFixture mssql)
    {
        _mssql = mssql;
    }

    [Fact]
    public void Fixture_is_available()
    {
        _mssql.ConnectionString.Should().NotBeNullOrWhiteSpace();
    }

    [Fact(Skip = "Phase 1: Functions Isolated Worker WebApplicationFactory wiring deferred to Stage Z.")]
    public void SearchSeries_returns_200_for_anonymous_caller()
    {
        // TODO: Boot ComiCal.Api via WebApplicationFactory<Program> and assert /api/series => 200 + ETag.
    }

    [Fact(Skip = "Phase 1: Functions Isolated Worker WebApplicationFactory wiring deferred to Stage Z.")]
    public void GetSeriesDetail_returns_404_problem_json_when_missing()
    {
        // TODO: Assert /api/series/{unknown-guid} => 404 application/problem+json with type uri.
    }
}
