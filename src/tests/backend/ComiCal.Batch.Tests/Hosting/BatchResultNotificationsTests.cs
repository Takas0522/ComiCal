using System.Net;
using System.Text.Json;
using ComiCal.Batch.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace ComiCal.Batch.Tests.Hosting;

public sealed class BatchResultNotificationsTests
{
    [Theory]
    [InlineData(0, false, "/ping/")]
    [InlineData(1, true, "/ping/fail")]
    [InlineData(2, true, "/ping/fail")]
    [InlineData(3, true, "/ping/fail")]
    [InlineData(4, false, null)]
    public async Task SendAsync_ExitCode_SelectsRequestsAndUsesSafePayload(int code, bool sendsWebhook, string? heartbeatPath)
    {
        using var handler = new RecordingHandler();
        var clients = Substitute.For<IHttpClientFactory>();
        clients.CreateClient("BatchOperations").Returns(_ => new HttpClient(handler, disposeHandler: false));
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Batch:FailureWebhookUrl"] = "https://example.test/webhook",
            ["Batch:HeartbeatUrl"] = "https://example.test/ping/?token=abc"
        }).Build();

        await new FailureNotifier(clients, config, TimeProvider.System, NullLogger<FailureNotifier>.Instance).SendAsync(code);
        await new Heartbeat(clients, config, NullLogger<Heartbeat>.Instance).SendAsync(code);

        Assert.Equal((sendsWebhook ? 1 : 0) + (heartbeatPath is null ? 0 : 1), handler.Requests.Count);
        if (sendsWebhook)
        {
            var post = handler.Requests[0];
            Assert.Equal(HttpMethod.Post, post.Method);
            Assert.Equal("/webhook", post.Uri.AbsolutePath);
            using var payload = JsonDocument.Parse(post.Body!);
            Assert.Equal(code, payload.RootElement.GetProperty("exitCode").GetInt32());
            Assert.Equal(ExitCodes.Summary(code), payload.RootElement.GetProperty("summary").GetString());
            Assert.True(payload.RootElement.GetProperty("timestamp").TryGetDateTimeOffset(out _));
            Assert.Contains($"終了コード {code}", payload.RootElement.GetProperty("text").GetString(), StringComparison.Ordinal);
            Assert.Equal(4, payload.RootElement.EnumerateObject().Count());
        }
        if (heartbeatPath is not null)
        {
            var get = handler.Requests[^1];
            Assert.Equal(HttpMethod.Get, get.Method);
            Assert.Equal(heartbeatPath, get.Uri.AbsolutePath);
            Assert.Equal("?token=abc", get.Uri.Query);
        }
    }

    [Fact]
    public async Task SendAsync_Unconfigured_DoesNotCreateHttpClient()
    {
        var clients = Substitute.For<IHttpClientFactory>();
        var config = new ConfigurationBuilder().Build();
        await new FailureNotifier(clients, config, TimeProvider.System, NullLogger<FailureNotifier>.Instance).SendAsync(1);
        await new Heartbeat(clients, config, NullLogger<Heartbeat>.Instance).SendAsync(1);
        Assert.Empty(clients.ReceivedCalls());
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public List<(HttpMethod Method, Uri Uri, string? Body)> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add((request.Method, request.RequestUri!, request.Content is null
                ? null : await request.Content.ReadAsStringAsync(cancellationToken)));
            return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
        }
    }
}
