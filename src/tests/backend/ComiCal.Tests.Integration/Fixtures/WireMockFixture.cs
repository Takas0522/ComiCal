using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using WireMock.Matchers;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using WireMock.Settings;
using Xunit;

namespace ComiCal.Tests.Integration.Fixtures;

/// <summary>
/// Boots an in-process WireMock.Net server preloaded with the mapping files in
/// <c>tools/wiremock/mappings/</c>. Each instance binds to a random free port so
/// it is safe to use in parallel xUnit collections (one per test class or
/// shared via <see cref="WireMockCollection"/>).
/// </summary>
/// <remarks>
/// The mapping JSON files use the standalone WireMock (Java) schema so that the
/// same files can be served by <c>tools/wiremock/scripts/run-wiremock.sh</c>
/// during local development. WireMock.Net's <c>ReadStaticMappings</c> expects a
/// slightly different schema, so this fixture parses the files manually and
/// registers the stubs through the WireMock.Net fluent API.
/// </remarks>
public sealed class WireMockFixture : IAsyncLifetime
{
    private WireMockServer? _server;

    public WireMockServer Server =>
        _server ?? throw new InvalidOperationException("WireMock server has not been started yet.");

    public string BaseUrl => Server.Url
        ?? throw new InvalidOperationException("WireMock server has no URL.");

    public string MappingsPath { get; } = ResolveMappingsPath();

    public ValueTask InitializeAsync()
    {
        _server = WireMockServer.Start(new WireMockServerSettings
        {
            // Port omitted ⇒ OS picks a free port; safe under parallel collections.
            StartAdminInterface = true,
        });

        foreach (var file in Directory.EnumerateFiles(MappingsPath, "*.json").OrderBy(f => f))
        {
            using var stream = File.OpenRead(file);
            using var doc = JsonDocument.Parse(stream);
            RegisterStub(_server, doc.RootElement);
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        _server?.Stop();
        _server?.Dispose();
        _server = null;
        return ValueTask.CompletedTask;
    }

    private static void RegisterStub(WireMockServer server, JsonElement root)
    {
        var request = root.GetProperty("request");
        var response = root.GetProperty("response");

        var method = request.GetProperty("method").GetString() ?? "GET";
        var urlPathPattern = request.GetProperty("urlPathPattern").GetString()
            ?? throw new InvalidOperationException("Mapping is missing request.urlPathPattern.");

        var requestBuilder = Request.Create()
            .UsingMethod(method)
            .WithPath(new RegexMatcher(urlPathPattern));

        if (request.TryGetProperty("queryParameters", out var qp))
        {
            foreach (var prop in qp.EnumerateObject())
            {
                IStringMatcher matcher = BuildStringMatcher(prop.Value);
                requestBuilder = requestBuilder.WithParam(prop.Name, matcher);
            }
        }

        var status = response.TryGetProperty("status", out var statusEl) ? statusEl.GetInt32() : 200;
        var responseBuilder = Response.Create().WithStatusCode(status);

        if (response.TryGetProperty("headers", out var headers))
        {
            foreach (var h in headers.EnumerateObject())
            {
                responseBuilder = responseBuilder.WithHeader(h.Name, h.Value.GetString() ?? string.Empty);
            }
        }

        if (response.TryGetProperty("jsonBody", out var jsonBody))
        {
            responseBuilder = responseBuilder.WithBody(jsonBody.GetRawText());
        }
        else if (response.TryGetProperty("body", out var body))
        {
            responseBuilder = responseBuilder.WithBody(body.GetString() ?? string.Empty);
        }

        var builder = server.Given(requestBuilder);
        if (root.TryGetProperty("priority", out var priorityEl))
        {
            builder = builder.AtPriority(priorityEl.GetInt32());
        }
        builder.RespondWith(responseBuilder);
    }

    private static IStringMatcher BuildStringMatcher(JsonElement el)
    {
        if (el.TryGetProperty("equalTo", out var eq))
        {
            return new ExactMatcher(eq.GetString() ?? string.Empty);
        }
        if (el.TryGetProperty("matches", out var rx))
        {
            return new RegexMatcher(rx.GetString() ?? ".*");
        }
        if (el.TryGetProperty("contains", out var co))
        {
            return new WildcardMatcher("*" + co.GetString() + "*");
        }
        throw new NotSupportedException(
            "Unsupported queryParameters matcher in WireMock mapping: " + el.GetRawText());
    }

    private static string ResolveMappingsPath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "tools", "wiremock", "mappings");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException(
            "Could not locate tools/wiremock/mappings/ above " + AppContext.BaseDirectory);
    }
}

[CollectionDefinition(Name)]
public sealed class WireMockCollection : ICollectionFixture<WireMockFixture>
{
    public const string Name = "WireMock";
}
