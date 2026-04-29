using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using ComiCal.Tests.Integration.Fixtures;
using FluentAssertions;
using Xunit;

namespace ComiCal.Tests.Integration;

[Trait("Category", "Integration")]
[Collection(WireMockCollection.Name)]
public sealed class RakutenWireMockTests
{
    private const string SearchPath = "/services/api/BooksTotal/Search/20170404";

    private readonly WireMockFixture _mock;

    public RakutenWireMockTests(WireMockFixture mock) => _mock = mock;

    private HttpClient NewClient() => new() { BaseAddress = new Uri(_mock.BaseUrl) };

    [Fact]
    public async Task Search_with_valid_keyword_returns_30_items()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = NewClient();
        var response = await client.GetAsync(
            $"{SearchPath}?keyword=ワンピース&bookGenreId=001001&applicationId=test", ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.GetProperty("count").GetInt32().Should().Be(30);
        root.GetProperty("Items").GetArrayLength().Should().Be(30);
    }

    [Fact]
    public async Task Search_with_ratelimit_sentinel_returns_429_with_retry_after()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = NewClient();
        var response = await client.GetAsync(
            $"{SearchPath}?keyword=__RATELIMIT__&bookGenreId=001001&applicationId=test", ct);

        response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        response.Headers.RetryAfter.Should().NotBeNull();
        response.Headers.RetryAfter!.Delta.Should().Be(TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task Search_with_no_results_sentinel_returns_empty_array()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = NewClient();
        var response = await client.GetAsync(
            $"{SearchPath}?keyword=__NORESULTS__&bookGenreId=001001&applicationId=test", ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.GetProperty("count").GetInt32().Should().Be(0);
        root.GetProperty("Items").GetArrayLength().Should().Be(0);
    }
}
