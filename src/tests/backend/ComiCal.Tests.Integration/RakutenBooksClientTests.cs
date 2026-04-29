using System.Diagnostics;
using ComiCal.Infrastructure.Rakuten;
using ComiCal.Tests.Integration.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ComiCal.Tests.Integration;

[Trait("Category", "Integration")]
[Collection(WireMockCollection.Name)]
public sealed class RakutenBooksClientTests
{
    private readonly WireMockFixture _mock;

    public RakutenBooksClientTests(WireMockFixture mock) => _mock = mock;

    private ServiceProvider BuildClient(int retryAttemptsBaseUrlOverride = 0)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Rakuten:BaseUrl"] = _mock.BaseUrl + "/",
                ["Rakuten:ApplicationId"] = "test-app-id",
                ["Rakuten:RatePerSecond"] = "1",
                ["Rakuten:TimeoutSeconds"] = "10",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddRakutenBooksClient(config);
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task SearchByGenreAsync_returns_30_items_from_fixture()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var sp = BuildClient();
        var client = sp.GetRequiredService<IRakutenBooksClient>();

        var result = await client.SearchByGenreAsync("ワンピース", 1, ct);

        result.Should().NotBeNull();
        result.Items.Should().HaveCount(30);
        result.Count.Should().Be(30);
    }

    [Fact]
    public async Task SearchByGenreAsync_with_no_results_sentinel_returns_empty()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var sp = BuildClient();
        var client = sp.GetRequiredService<IRakutenBooksClient>();

        var result = await client.SearchByGenreAsync("__NORESULTS__", 1, ct);

        result.Items.Should().BeEmpty();
        result.Count.Should().Be(0);
    }

    [Fact]
    public async Task SearchByGenreAsync_with_ratelimit_sentinel_throws_after_retries()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var sp = BuildClient();
        var client = sp.GetRequiredService<IRakutenBooksClient>();

        var act = async () => await client.SearchByGenreAsync("__RATELIMIT__", 1, ct);

        var ex = await act.Should().ThrowAsync<RakutenBooksApiException>();
        ex.Which.StatusCode.Should().Be(429);
    }

    [Fact]
    public async Task SearchByGenreAsync_with_500_sentinel_throws_after_retries()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var sp = BuildClient();
        var client = sp.GetRequiredService<IRakutenBooksClient>();

        var act = async () => await client.SearchByGenreAsync("__500__", 1, ct);

        var ex = await act.Should().ThrowAsync<RakutenBooksApiException>();
        ex.Which.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task RateLimiter_serializes_three_calls_to_at_least_two_seconds()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var sp = BuildClient();
        var client = sp.GetRequiredService<IRakutenBooksClient>();

        var sw = Stopwatch.StartNew();
        for (var i = 0; i < 3; i++)
        {
            _ = await client.SearchByGenreAsync("ワンピース", 1, ct);
        }
        sw.Stop();

        // TokenBucket: 1 token capacity, 1 token / sec → 3 sequential requests need ≥ ~2 s
        // (first immediate, second after 1s, third after 2s). Allow some slack to avoid flakes.
        sw.Elapsed.Should().BeGreaterThanOrEqualTo(TimeSpan.FromMilliseconds(1800));
    }
}
