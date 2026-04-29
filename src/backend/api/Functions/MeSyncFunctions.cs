using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using ComiCal.Api.Common;
using ComiCal.Api.Middleware;
using ComiCal.Api.ProblemDetails;
using ComiCal.Application.Common;
using ComiCal.Application.DTOs;
using ComiCal.Application.UseCases.Me;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;

namespace ComiCal.Api.Functions;

/// <summary>
/// HTTP triggers for <c>/api/me/sync/qr</c>. Phase 2 cross-device sync flow:
/// the authenticated origin device (A) issues a short-lived (5 min) one-time
/// token; the second device (B) — already authenticated via SWA on its own —
/// redeems the token to confirm "same logical user" intent.
/// </summary>
[RequiresAuthenticatedUser]
public sealed class MeSyncFunctions(
    ILogger<MeSyncFunctions> logger,
    IIssueSyncTokenUseCase issueUseCase,
    IRedeemSyncTokenUseCase redeemUseCase,
    ICurrentUser currentUser,
    ProblemDetailsFactory problemDetailsFactory)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ILogger<MeSyncFunctions> _logger = logger;
    private readonly IIssueSyncTokenUseCase _issueUseCase = issueUseCase;
    private readonly IRedeemSyncTokenUseCase _redeemUseCase = redeemUseCase;
    private readonly ICurrentUser _currentUser = currentUser;
    private readonly ProblemDetailsFactory _problemDetailsFactory = problemDetailsFactory;

    [Function("IssueMeSyncToken")]
    [OpenApiOperation(operationId: "IssueMeSyncToken", tags: ["Me", "Sync"], Summary = "Issue a QR sync token", Description = "Generates a short-lived (5 min) one-time token bound to the authenticated user. The plaintext token is returned exactly once and embedded in the QR payload URL.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(SyncTokenIssuedDto), Summary = "Token issued. Plaintext is included in the response and must not be logged.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Unauthorized, contentType: "application/problem+json", bodyType: typeof(ProblemDetailsBody), Summary = "Authentication required.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.TooManyRequests, contentType: "application/problem+json", bodyType: typeof(ProblemDetailsBody), Summary = "Rate limit exceeded.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.InternalServerError, contentType: "application/problem+json", bodyType: typeof(ProblemDetailsBody), Summary = "Unexpected error.")]
    public async Task<IActionResult> IssueAsync(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "me/sync/qr")] HttpRequest request,
        FunctionContext executionContext)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(executionContext);

        _logger.LogInformation("Issuing sync token for {UserId}", _currentUser.Id);

        var baseUrl = ResolveOrigin(request);
        var result = await _issueUseCase.ExecuteAsync(
            new IssueSyncTokenCommand(baseUrl),
            BuildContext(executionContext),
            request.HttpContext.RequestAborted).ConfigureAwait(false);

        return await UseCaseResultMapper.ToActionResultAsync(
            result, request, executionContext, _problemDetailsFactory, useEtag: false).ConfigureAwait(false);
    }

    [Function("RedeemMeSyncToken")]
    [OpenApiOperation(operationId: "RedeemMeSyncToken", tags: ["Me", "Sync"], Summary = "Redeem a QR sync token", Description = "Validates and consumes a sync token issued by the same logical user from another device. Returns 204 on success.")]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(RedeemSyncTokenRequest), Required = true, Description = "Token redemption request.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NoContent, Summary = "Token consumed.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/problem+json", bodyType: typeof(ProblemDetailsBody), Summary = "Invalid body.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Unauthorized, contentType: "application/problem+json", bodyType: typeof(ProblemDetailsBody), Summary = "Authentication required.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Forbidden, contentType: "application/problem+json", bodyType: typeof(ProblemDetailsBody), Summary = "Token belongs to a different user.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "application/problem+json", bodyType: typeof(ProblemDetailsBody), Summary = "Token not found or expired.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Conflict, contentType: "application/problem+json", bodyType: typeof(ProblemDetailsBody), Summary = "Token already consumed.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.TooManyRequests, contentType: "application/problem+json", bodyType: typeof(ProblemDetailsBody), Summary = "Rate limit exceeded.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.InternalServerError, contentType: "application/problem+json", bodyType: typeof(ProblemDetailsBody), Summary = "Unexpected error.")]
    public async Task<IActionResult> RedeemAsync(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "me/sync/qr/redeem")] HttpRequest request,
        FunctionContext executionContext)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(executionContext);

        var body = await DeserializeBodyAsync<RedeemSyncTokenRequest>(request).ConfigureAwait(false);
        if (body is null)
        {
            throw new ValidationException("Request body is required.");
        }

        // Token plaintext is intentionally not logged.
        _logger.LogInformation("Redeeming sync token for {UserId}", _currentUser.Id);

        var result = await _redeemUseCase.ExecuteAsync(
            new RedeemSyncTokenCommand(body.Token),
            BuildContext(executionContext),
            request.HttpContext.RequestAborted).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return UseCaseResultMapper.ToProblem(result.Error, request, executionContext, _problemDetailsFactory);
        }
        return new NoContentResult();
    }

    private UseCaseContext BuildContext(FunctionContext executionContext)
        => new(
            UserId: _currentUser.IsAuthenticated ? _currentUser.Id : null,
            CorrelationId: CorrelationContextAccessor.GetCorrelationId(executionContext) ?? executionContext.InvocationId);

    /// <summary>
    /// Resolve the public origin (scheme + host) the user actually saw, honouring
    /// SWA / reverse-proxy <c>X-Forwarded-*</c> headers. Falls back to the request URL.
    /// </summary>
    private static string ResolveOrigin(HttpRequest request)
    {
        var scheme = FirstHeader(request, "X-Forwarded-Proto") ?? request.Scheme;
        var host = FirstHeader(request, "X-Forwarded-Host") ?? request.Host.ToString();
        if (string.IsNullOrWhiteSpace(host))
        {
            host = "localhost";
        }
        return $"{scheme}://{host}";
    }

    private static string? FirstHeader(HttpRequest request, string name)
    {
        if (!request.Headers.TryGetValue(name, out var values))
        {
            return null;
        }
        var raw = values.ToString();
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }
        // X-Forwarded-* may be comma-separated; first value is the original client-facing one.
        var idx = raw.IndexOf(',');
        return (idx >= 0 ? raw[..idx] : raw).Trim();
    }

    private static async Task<T?> DeserializeBodyAsync<T>(HttpRequest request)
    {
        if (request.Body is null)
        {
            return default;
        }
        try
        {
            return await JsonSerializer.DeserializeAsync<T>(
                request.Body, JsonOptions, request.HttpContext.RequestAborted).ConfigureAwait(false);
        }
        catch (JsonException ex)
        {
            throw new ValidationException("Request body is not valid JSON: " + ex.Message);
        }
    }
}

/// <summary>POST <c>/api/me/sync/qr/redeem</c> request body.</summary>
public sealed record RedeemSyncTokenRequest(string Token);
