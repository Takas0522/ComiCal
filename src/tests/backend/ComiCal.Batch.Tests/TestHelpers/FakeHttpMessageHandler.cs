using System.Net;
using System.Text;

namespace ComiCal.Batch.Tests.TestHelpers;

/// <summary>Intercepts outgoing HTTP calls and returns a pre-configured JSON response.</summary>
internal sealed class FakeHttpMessageHandler(string responseJson, HttpStatusCode statusCode = HttpStatusCode.OK)
    : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
        => Task.FromResult(new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json"),
        });
}
