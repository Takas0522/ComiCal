using ComiCal.Infrastructure.Rakuten;
using Microsoft.Extensions.Logging.Abstractions;
using System.Net;
using System.Threading.RateLimiting;
using Xunit;

namespace ComiCal.Batch.Tests.Activities;

public sealed class RakutenBooksApiClientTests
{
    [Fact]
    public async Task SearchComicsAsync_WhenRakutenReturns503_ThrowsWithStatusCode()
    {
        using var rateLimiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
        {
            PermitLimit = 1,
            Window = TimeSpan.FromSeconds(1),
            SegmentsPerWindow = 1,
        });
        using var client = new HttpClient(new StaticResponseHandler(
            HttpStatusCode.ServiceUnavailable,
            """{"error":"service temporarily unavailable","applicationId":"must-not-be-logged"}"""));
        client.DefaultRequestHeaders.Add("X-Rakuten-AppId", "test-app-id");
        var sut = new RakutenBooksApiClient(
            client,
            rateLimiter,
            NullLogger<RakutenBooksApiClient>.Instance);

        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            () => sut.SearchComicsAsync(1, null, null));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, exception.StatusCode);
    }

    private sealed class StaticResponseHandler(HttpStatusCode statusCode, string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(body),
            });
    }
}
