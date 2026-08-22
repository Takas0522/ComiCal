using ComiCal.Infrastructure.Rakuten;
using Microsoft.Extensions.Logging;
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
            NullLogger<RakutenBooksApiClient>.Instance,
            new RakutenAuthCredentials("test-id", "test-key", "test-affiliate"));

        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            () => sut.SearchComicsAsync(1, null, null));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, exception.StatusCode);
    }

    [Fact]
    public async Task SearchComicsAsync_WhenRakutenReturns403_ThrowsWithForbiddenStatus()
    {
        using var rateLimiter = CreateRateLimiter();
        using var client = new HttpClient(new StaticResponseHandler(
            HttpStatusCode.Forbidden,
            """{"error":"wrong_parameter","error_description":"applicationId is invalid"}"""));
        var sut = new RakutenBooksApiClient(
            client,
            rateLimiter,
            NullLogger<RakutenBooksApiClient>.Instance,
            new RakutenAuthCredentials("test-id", "test-key", "test-affiliate"));

        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            () => sut.SearchComicsAsync(1, null, null));

        Assert.Equal(HttpStatusCode.Forbidden, exception.StatusCode);
    }

    [Fact]
    public async Task SearchComicsAsync_WhenRakutenReturnsNestedError_ExtractsDiagnostic()
    {
        // Rakuten sometimes wraps the error inside a Body envelope.
        using var rateLimiter = CreateRateLimiter();
        var (logger, sink) = CreateCapturingLogger();
        using var client = new HttpClient(new StaticResponseHandler(
            HttpStatusCode.Forbidden,
            """{"Header":{"Status":403},"Body":{"error":"forbidden","error_description":"application quota exceeded"}}"""));
        var sut = new RakutenBooksApiClient(
            client,
            rateLimiter,
            logger,
            new RakutenAuthCredentials("test-id", "test-key", "test-affiliate"));

        await Assert.ThrowsAsync<HttpRequestException>(
            () => sut.SearchComicsAsync(1, null, null));

        Assert.Contains(sink, entry => entry.Contains("application quota exceeded", StringComparison.Ordinal));
    }

    [Fact]
    public async Task SearchComicsAsync_WhenNoRecognizedField_IncludesBodyPrefixInDiagnostic()
    {
        using var rateLimiter = CreateRateLimiter();
        var (logger, sink) = CreateCapturingLogger();
        using var client = new HttpClient(new StaticResponseHandler(
            HttpStatusCode.Forbidden,
            """{"foo":"bar","baz":42}"""));
        var sut = new RakutenBooksApiClient(
            client,
            rateLimiter,
            logger,
            new RakutenAuthCredentials("test-id", "test-key", "test-affiliate"));

        await Assert.ThrowsAsync<HttpRequestException>(
            () => sut.SearchComicsAsync(1, null, null));

        // Fallback must include a raw body snapshot so operators can see the payload.
        Assert.Contains(sink, entry => entry.Contains("bodyPrefix=", StringComparison.Ordinal)
            && entry.Contains("\"foo\":\"bar\"", StringComparison.Ordinal));
    }

    [Fact]
    public async Task SearchComicsAsync_WhenNonJsonBody_IncludesBodyPrefixInDiagnostic()
    {
        using var rateLimiter = CreateRateLimiter();
        var (logger, sink) = CreateCapturingLogger();
        using var client = new HttpClient(new StaticResponseHandler(
            HttpStatusCode.Forbidden,
            "<html><body>Access denied by upstream firewall</body></html>",
            mediaType: "text/html"));
        var sut = new RakutenBooksApiClient(
            client,
            rateLimiter,
            logger,
            new RakutenAuthCredentials("test-id", "test-key", "test-affiliate"));

        await Assert.ThrowsAsync<HttpRequestException>(
            () => sut.SearchComicsAsync(1, null, null));

        Assert.Contains(sink, entry => entry.Contains("Access denied by upstream firewall", StringComparison.Ordinal));
    }

    [Fact]
    public async Task SearchComicsAsync_WhenRakutenReturnsRakutenErrorEnvelope_ExtractsErrorMessage()
    {
        // Actual production 403 payload observed on 2026-08-15 through 2026-08-21:
        // {"errors":{"errorCode":403,"errorMessage":"CLIENT_IP_NOT_ALLOWED"}}
        using var rateLimiter = CreateRateLimiter();
        var (logger, sink) = CreateCapturingLogger();
        using var client = new HttpClient(new StaticResponseHandler(
            HttpStatusCode.Forbidden,
            """{"errors":{"errorCode":403,"errorMessage":"CLIENT_IP_NOT_ALLOWED"}}"""));
        var sut = new RakutenBooksApiClient(
            client,
            rateLimiter,
            logger,
            new RakutenAuthCredentials("test-id", "test-key", "test-affiliate"));

        await Assert.ThrowsAsync<HttpRequestException>(
            () => sut.SearchComicsAsync(1, null, null));

        Assert.Contains(sink, entry => entry.Contains("CLIENT_IP_NOT_ALLOWED", StringComparison.Ordinal));
    }

    private static SlidingWindowRateLimiter CreateRateLimiter()
        => new(new SlidingWindowRateLimiterOptions
        {
            PermitLimit = 1,
            Window = TimeSpan.FromSeconds(1),
            SegmentsPerWindow = 1,
        });

    private static (ILogger<RakutenBooksApiClient> logger, List<string> sink) CreateCapturingLogger()
    {
        var sink = new List<string>();
        var factory = LoggerFactory.Create(builder => builder.AddProvider(new ListLoggerProvider(sink)));
        return (factory.CreateLogger<RakutenBooksApiClient>(), sink);
    }

    private sealed class StaticResponseHandler(HttpStatusCode statusCode, string body, string mediaType = "application/json") : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(body, System.Text.Encoding.UTF8, mediaType),
            });
    }

    private sealed class ListLoggerProvider(List<string> sink) : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => new ListLogger(sink);
        public void Dispose() { }

        private sealed class ListLogger(List<string> sink) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel logLevel) => true;
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                lock (sink) { sink.Add(formatter(state, exception)); }
            }
        }
    }
}
