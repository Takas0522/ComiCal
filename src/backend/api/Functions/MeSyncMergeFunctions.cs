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
/// HTTP trigger for <c>POST /api/me/sync/merge</c> — Phase 2 anonymous→authenticated
/// data merge. Accepts the SPA's local IndexedDB payload (subscriptions / purchases)
/// and idempotently UPSERTs each item against the authenticated user's records inside
/// a single DB transaction. Items pointing to non-existent series / volume IDs are
/// returned as <c>skipped</c> rather than failing the whole request.
/// </summary>
[RequiresAuthenticatedUser]
public sealed class MeSyncMergeFunctions(
    ILogger<MeSyncMergeFunctions> logger,
    IMergeAnonymousDataUseCase mergeUseCase,
    ICurrentUser currentUser,
    ProblemDetailsFactory problemDetailsFactory)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ILogger<MeSyncMergeFunctions> _logger = logger;
    private readonly IMergeAnonymousDataUseCase _mergeUseCase = mergeUseCase;
    private readonly ICurrentUser _currentUser = currentUser;
    private readonly ProblemDetailsFactory _problemDetailsFactory = problemDetailsFactory;

    [Function("MergeAnonymousData")]
    [OpenApiOperation(operationId: "MergeAnonymousData", tags: ["Me", "Sync"], Summary = "Merge anonymous local data into the signed-in account", Description = "Idempotent UPSERT of the SPA's local subscriptions and purchases against the authenticated user. Items with unknown SeriesId/VolumeId are reported as skipped.")]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(MergeAnonymousDataRequest), Required = true, Description = "Anonymous payload exported from the local IndexedDB store.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(MergeResultDto), Summary = "Merge complete; counts and skipped IDs reported.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/problem+json", bodyType: typeof(ProblemDetailsBody), Summary = "Invalid body (oversized payload or malformed item).")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Unauthorized, contentType: "application/problem+json", bodyType: typeof(ProblemDetailsBody), Summary = "Authentication required.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.TooManyRequests, contentType: "application/problem+json", bodyType: typeof(ProblemDetailsBody), Summary = "Rate limit exceeded.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.InternalServerError, contentType: "application/problem+json", bodyType: typeof(ProblemDetailsBody), Summary = "Transaction failed.")]
    public async Task<IActionResult> MergeAsync(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "me/sync/merge")] HttpRequest request,
        FunctionContext executionContext)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(executionContext);

        var body = await DeserializeBodyAsync<MergeAnonymousDataRequest>(request).ConfigureAwait(false);
        if (body is null)
        {
            throw new ValidationException("Request body is required.");
        }

        var command = new MergeAnonymousDataCommand(
            body.Subscriptions ?? Array.Empty<MergeAnonymousSubscriptionItem>(),
            body.Purchases ?? Array.Empty<MergeAnonymousPurchaseItem>());

        _logger.LogInformation(
            "Merging anonymous data for {UserId}: {SubCount} subs, {PurchaseCount} purchases",
            _currentUser.Id, command.Subscriptions.Count, command.Purchases.Count);

        var result = await _mergeUseCase.ExecuteAsync(
            command,
            BuildContext(executionContext),
            request.HttpContext.RequestAborted).ConfigureAwait(false);

        return await UseCaseResultMapper.ToActionResultAsync(
            result, request, executionContext, _problemDetailsFactory, useEtag: false).ConfigureAwait(false);
    }

    private UseCaseContext BuildContext(FunctionContext executionContext)
        => new(
            UserId: _currentUser.IsAuthenticated ? _currentUser.Id : null,
            CorrelationId: CorrelationContextAccessor.GetCorrelationId(executionContext) ?? executionContext.InvocationId);

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
