using ComiCal.Tests.Integration.Fixtures;
using FluentAssertions;
using Xunit;

namespace ComiCal.Tests.Integration.Api;

/// <summary>
/// Phase 2 placeholder for <c>DELETE /api/me/account</c> integration tests.
/// Hosting the Functions Isolated Worker inside <c>WebApplicationFactory</c>
/// is deferred to Stage Z (same rationale as <see cref="SeriesEndpointTests"/>);
/// the Application- and Domain-layer behaviour is covered by
/// <c>ComiCal.Tests.Unit.Application.Me.DeleteAccountUseCaseTests</c>.
/// </summary>
[Collection(ApiIntegrationCollection.Name)]
[Trait("Category", "Integration")]
public sealed class MeAccountEndpointTests
{
    private readonly MsSqlFixture _mssql;

    public MeAccountEndpointTests(MsSqlFixture mssql)
    {
        _mssql = mssql;
    }

    [Fact]
    public void Fixture_is_available()
    {
        _mssql.ConnectionString.Should().NotBeNullOrWhiteSpace();
    }

    [Fact(Skip = "Stage Z hardening: requires Testcontainers + DACPAC publish in CI.")]
    public void Delete_hard_removes_user_and_all_fk_rows()
    {
        // TODO: seed Users + Subscriptions + Purchases + IdentityLinks for a user;
        // DELETE /api/me/account → 204 with X-Logout-Required: true;
        // assert all four tables no longer contain the user id.
    }

    [Fact(Skip = "Stage Z hardening: requires Testcontainers + DACPAC publish in CI.")]
    public void Delete_is_idempotent_when_user_already_absent()
    {
        // TODO: call DELETE /api/me/account twice; both must return 204.
    }

    [Fact(Skip = "Stage Z hardening: requires Testcontainers + DACPAC publish in CI.")]
    public void Anonymous_caller_gets_401_problem_json()
    {
        // TODO: assert 401 application/problem+json without x-ms-client-principal header.
    }
}
