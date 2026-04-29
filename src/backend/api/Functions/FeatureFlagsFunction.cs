using System.Net;
using System.Threading.Tasks;
using ComiCal.Infrastructure.AppConfig;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;

namespace ComiCal.Api.Functions;

/// <summary>
/// Returns the materialised feature flag map used by the SPA / SSR shell at
/// bootstrap time. The response intentionally contains no PII so it is safe
/// to cache and serve to anonymous callers via the SWA-linked backend.
/// </summary>
public sealed class FeatureFlagsFunction(
    ILogger<FeatureFlagsFunction> logger,
    IFeatureFlagProvider featureFlagProvider)
{
    private readonly ILogger<FeatureFlagsFunction> _logger = logger;
    private readonly IFeatureFlagProvider _featureFlagProvider = featureFlagProvider;

    [Function("FeatureFlags")]
    [OpenApiOperation(
        operationId: "getFeatureFlags",
        tags: ["feature-flags"],
        Summary = "Get all feature flags",
        Description = "Returns a map of feature flag names to their enabled state. Anonymous-safe; suitable for SPA/SSR bootstrap.")]
    [OpenApiResponseWithBody(
        statusCode: HttpStatusCode.OK,
        contentType: "application/json",
        bodyType: typeof(IReadOnlyDictionary<string, bool>),
        Summary = "Feature flag map keyed by flag name.")]
    public async Task<IActionResult> RunAsync(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "feature-flags")] HttpRequest request,
        FunctionContext executionContext)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(executionContext);

        var ct = request.HttpContext.RequestAborted;
        var flags = await _featureFlagProvider.GetAllAsync(ct).ConfigureAwait(false);

        _logger.LogInformation(
            "Feature flags resolved {FlagCount} entries {InvocationId}",
            flags.Count,
            executionContext.InvocationId);

        return new OkObjectResult(flags);
    }
}
