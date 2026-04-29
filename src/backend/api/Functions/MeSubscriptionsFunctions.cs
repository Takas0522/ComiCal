using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using ComiCal.Api.Common;
using ComiCal.Api.Middleware;
using ComiCal.Api.ProblemDetails;
using ComiCal.Application.Common;
using ComiCal.Application.DTOs;
using ComiCal.Application.UseCases.Me;
using ComiCal.Shared;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;

namespace ComiCal.Api.Functions;

/// <summary>
/// HTTP triggers for <c>/api/me/subscriptions</c> (Phase 2 authenticated user APIs).
/// All endpoints require an authenticated SWA principal — gated by
/// <see cref="RequiresAuthenticatedUserAttribute"/> and the
/// <c>SwaAuthMiddleware</c> / <c>CurrentUserResolverMiddleware</c> pair in
/// <c>Program.cs</c>. The user identifier is read from <see cref="ICurrentUser.Id"/>;
/// request bodies must NOT carry a userId.
/// </summary>
[RequiresAuthenticatedUser]
public sealed class MeSubscriptionsFunctions(
    ILogger<MeSubscriptionsFunctions> logger,
    IListSubscriptionsUseCase listUseCase,
    IAddSubscriptionUseCase addUseCase,
    IRemoveSubscriptionUseCase removeUseCase,
    ICurrentUser currentUser,
    ProblemDetailsFactory problemDetailsFactory)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ILogger<MeSubscriptionsFunctions> _logger = logger;
    private readonly IListSubscriptionsUseCase _listUseCase = listUseCase;
    private readonly IAddSubscriptionUseCase _addUseCase = addUseCase;
    private readonly IRemoveSubscriptionUseCase _removeUseCase = removeUseCase;
    private readonly ICurrentUser _currentUser = currentUser;
    private readonly ProblemDetailsFactory _problemDetailsFactory = problemDetailsFactory;

    [Function("ListMeSubscriptions")]
    [OpenApiOperation(operationId: "ListMeSubscriptions", tags: ["Me", "Subscriptions"], Summary = "List my subscriptions", Description = "Returns the authenticated user's active series subscriptions.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(SubscriptionListDto), Summary = "Subscription list with weak ETag.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NotModified, Summary = "ETag matched If-None-Match.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Unauthorized, contentType: "application/problem+json", bodyType: typeof(ProblemDetailsBody), Summary = "Authentication required.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.TooManyRequests, contentType: "application/problem+json", bodyType: typeof(ProblemDetailsBody), Summary = "Rate limit exceeded.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.InternalServerError, contentType: "application/problem+json", bodyType: typeof(ProblemDetailsBody), Summary = "Unexpected error.")]
    public async Task<IActionResult> ListAsync(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "me/subscriptions")] HttpRequest request,
        FunctionContext executionContext)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(executionContext);

        _logger.LogInformation("Listing subscriptions for {UserId}", _currentUser.Id);
        var result = await _listUseCase.ExecuteAsync(
            new ListSubscriptionsQuery(),
            BuildContext(executionContext),
            request.HttpContext.RequestAborted).ConfigureAwait(false);

        return await UseCaseResultMapper.ToActionResultAsync(
            result, request, executionContext, _problemDetailsFactory, useEtag: true).ConfigureAwait(false);
    }

    [Function("AddMeSubscription")]
    [OpenApiOperation(operationId: "AddMeSubscription", tags: ["Me", "Subscriptions"], Summary = "Subscribe to a series", Description = "Idempotent UPSERT keyed on (UserId, SeriesId). 201 on insert, 200 if already subscribed.")]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(AddSubscriptionRequest), Required = true, Description = "Series subscription request.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Created, contentType: "application/json", bodyType: typeof(SubscriptionDto), Summary = "New subscription created.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(SubscriptionDto), Summary = "Existing subscription returned (idempotent).")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/problem+json", bodyType: typeof(ProblemDetailsBody), Summary = "Invalid body.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Unauthorized, contentType: "application/problem+json", bodyType: typeof(ProblemDetailsBody), Summary = "Authentication required.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "application/problem+json", bodyType: typeof(ProblemDetailsBody), Summary = "Target series does not exist.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.TooManyRequests, contentType: "application/problem+json", bodyType: typeof(ProblemDetailsBody), Summary = "Rate limit exceeded.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.InternalServerError, contentType: "application/problem+json", bodyType: typeof(ProblemDetailsBody), Summary = "Unexpected error.")]
    public async Task<IActionResult> AddAsync(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "me/subscriptions")] HttpRequest request,
        FunctionContext executionContext)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(executionContext);

        var body = await DeserializeBodyAsync<AddSubscriptionRequest>(request).ConfigureAwait(false);
        if (body is null)
        {
            throw new ValidationException("Request body is required.");
        }

        var command = new AddSubscriptionCommand(body.SeriesId);
        _logger.LogInformation("Adding subscription for {UserId} to {SeriesId}", _currentUser.Id, body.SeriesId);

        var result = await _addUseCase.ExecuteAsync(
            command,
            BuildContext(executionContext),
            request.HttpContext.RequestAborted).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return UseCaseResultMapper.ToProblem(result.Error, request, executionContext, _problemDetailsFactory);
        }

        var payload = result.Value!;
        return new ObjectResult(payload.Subscription)
        {
            StatusCode = payload.Created ? StatusCodes.Status201Created : StatusCodes.Status200OK,
        };
    }

    [Function("RemoveMeSubscription")]
    [OpenApiOperation(operationId: "RemoveMeSubscription", tags: ["Me", "Subscriptions"], Summary = "Unsubscribe from a series", Description = "Soft-deletes the (UserId, SeriesId) subscription. Returns 204.")]
    [OpenApiParameter(name: "seriesId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid), Description = "Series identifier (GUID).")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NoContent, Summary = "Subscription deleted (or already absent).")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Unauthorized, contentType: "application/problem+json", bodyType: typeof(ProblemDetailsBody), Summary = "Authentication required.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "application/problem+json", bodyType: typeof(ProblemDetailsBody), Summary = "Subscription not found.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.TooManyRequests, contentType: "application/problem+json", bodyType: typeof(ProblemDetailsBody), Summary = "Rate limit exceeded.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.InternalServerError, contentType: "application/problem+json", bodyType: typeof(ProblemDetailsBody), Summary = "Unexpected error.")]
    public async Task<IActionResult> RemoveAsync(
        [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "me/subscriptions/{seriesId:guid}")] HttpRequest request,
        Guid seriesId,
        FunctionContext executionContext)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(executionContext);

        _logger.LogInformation("Removing subscription for {UserId} from {SeriesId}", _currentUser.Id, seriesId);

        var result = await _removeUseCase.ExecuteAsync(
            new RemoveSubscriptionCommand(seriesId),
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

/// <summary>POST <c>/api/me/subscriptions</c> request body.</summary>
public sealed record AddSubscriptionRequest(Guid SeriesId);
