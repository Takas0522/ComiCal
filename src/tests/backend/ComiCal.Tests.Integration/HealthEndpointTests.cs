using ComiCal.Tests.Integration.Fixtures;
using FluentAssertions;
using Xunit;

namespace ComiCal.Tests.Integration;

[Collection(ApiIntegrationCollection.Name)]
[Trait("Category", "Integration")]
public sealed class HealthEndpointTests
{
    private readonly MsSqlFixture _mssql;
    private readonly AzuriteFixture _azurite;

    public HealthEndpointTests(MsSqlFixture mssql, AzuriteFixture azurite)
    {
        _mssql = mssql;
        _azurite = azurite;
    }

    [Fact]
    public void Fixtures_AreAvailable()
    {
        _mssql.ConnectionString.Should().NotBeNullOrWhiteSpace();
        _azurite.ConnectionString.Should().NotBeNullOrWhiteSpace();
    }

    [Fact(Skip = "Phase 1: Functions Isolated Worker WebApplicationFactory wiring deferred to Stage G.")]
    public void Health_ReturnsOk()
    {
        // TODO: Boot ComiCal.Api via WebApplicationFactory<Program> and assert /api/health => 200.
    }
}
