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
/// HTTP triggers for <c>/api/me/purchases</c> (Phase 2 authenticated user APIs).
/// All endpoints require an authenticated SWA principal. The user identifier is
/// read from <see cref="ICurrentUser.Id"/>; request bodies must NOT carry a userId.
/// </summary>
[RequiresAuthenticatedUser]
public sealed class MePurchasesFunctions(
    ILogger<MePurchasesFunctions> logger,
    IListPurchasesUseCase listUseCase,
    IAddPurchaseUseCase addUseCase,
    IRemovePurchaseUseCase removeUseCase,
    ICurrentUser currentUser,
    ProblemDetailsFactory problemDetailsFactory)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ILogger<MePurchasesFunctions> _logger = logger;
    private readonly IListPurchasesUseCase _listUseCase = listUseCase;
    private readonly IAddPurchaseUseCase _addUseCase = addUseCase;
    private readonly IRemovePurchaseUseCase _removeUseCase = removeUseCase;
    private readonly ICurrentUser _currentUser = currentUser;
    private readonly ProblemDetailsFactory _problemDetailsFactory = problemDetailsFactory;

    [Function("ListMePurchases")]
    [OpenApiOperation(operationId: "ListMePurchases", tags: ["Me", "Purchases"], Summary = "List my purchases", Description = "Returns the authenticated user's recorded volume purchases.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(PurchaseListDto), Summary = "Purchase list with weak ETag.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NotModified, Summary = "ETag matched If-None-Match.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Unauthorized, contentType: "application/problem+json", bodyType: typeof(ProblemDetailsBody), Summary = "Authentication required.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.TooManyRequests, contentType: "application/problem+json", bodyType: typeof(ProblemDetailsBody), Summary = "Rate limit exceeded.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.InternalServerError, contentType: "application/problem+json", bodyType: typeof(ProblemDetailsBody), Summary = "Unexpected error.")]
    public async Task<IActionResult> ListAsync(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "me/purchases")] HttpRequest request,
        FunctionContext executionContext)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(executionContext);

        _logger.LogInformation("Listing purchases for {UserId}", _currentUser.Id);
        var result = await _listUseCase.ExecuteAsync(
            new ListPurchasesQuery(),
            BuildContext(executionContext),
            request.HttpContext.RequestAborted).ConfigureAwait(false);

        return await UseCaseResultMapper.ToActionResultAsync(
            result, request, executionContext, _problemDetailsFactory, useEtag: true).ConfigureAwait(false);
    }

    [Function("AddMePurchase")]
    [OpenApiOperation(operationId: "AddMePurchase", tags: ["Me", "Purchases"], Summary = "Record a purchase", Description = "Idempotent UPSERT keyed on (UserId, VolumeId). 201 on insert, 200 on update.")]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(AddPurchaseRequest), Required = true, Description = "Purchase request.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Created, contentType: "application/json", bodyType: typeof(PurchaseDto), Summary = "Purchase recorded.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(PurchaseDto), Summary = "Existing purchase updated (idempotent).")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/problem+json", bodyType: typeof(ProblemDetailsBody), Summary = "Invalid body.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Unauthorized, contentType: "application/problem+json", bodyType: typeof(ProblemDetailsBody), Summary = "Authentication required.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "application/problem+json", bodyType: typeof(ProblemDetailsBody), Summary = "Target volume does not exist.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.TooManyRequests, contentType: "application/problem+json", bodyType: typeof(ProblemDetailsBody), Summary = "Rate limit exceeded.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.InternalServerError, contentType: "application/problem+json", bodyType: typeof(ProblemDetailsBody), Summary = "Unexpected error.")]
    public async Task<IActionResult> AddAsync(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "me/purchases")] HttpRequest request,
        FunctionContext executionContext)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(executionContext);

        var body = await DeserializeBodyAsync<AddPurchaseRequest>(request).ConfigureAwait(false);
        if (body is null)
        {
            throw new ValidationException("Request body is required.");
        }

        var command = new AddPurchaseCommand(body.VolumeId, body.PurchasedAt);
        _logger.LogInformation("Adding purchase for {UserId} of {VolumeId}", _currentUser.Id, body.VolumeId);

        var result = await _addUseCase.ExecuteAsync(
            command,
            BuildContext(executionContext),
            request.HttpContext.RequestAborted).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return UseCaseResultMapper.ToProblem(result.Error, request, executionContext, _problemDetailsFactory);
        }

        var payload = result.Value!;
        return new ObjectResult(payload.Purchase)
        {
            StatusCode = payload.Created ? StatusCodes.Status201Created : StatusCodes.Status200OK,
        };
    }

    [Function("RemoveMePurchase")]
    [OpenApiOperation(operationId: "RemoveMePurchase", tags: ["Me", "Purchases"], Summary = "Remove a purchase record", Description = "Soft-deletes the (UserId, VolumeId) purchase. Returns 204.")]
    [OpenApiParameter(name: "volumeId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid), Description = "Volume identifier (GUID).")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NoContent, Summary = "Purchase removed.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Unauthorized, contentType: "application/problem+json", bodyType: typeof(ProblemDetailsBody), Summary = "Authentication required.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "application/problem+json", bodyType: typeof(ProblemDetailsBody), Summary = "Purchase not found.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.TooManyRequests, contentType: "application/problem+json", bodyType: typeof(ProblemDetailsBody), Summary = "Rate limit exceeded.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.InternalServerError, contentType: "application/problem+json", bodyType: typeof(ProblemDetailsBody), Summary = "Unexpected error.")]
    public async Task<IActionResult> RemoveAsync(
        [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "me/purchases/{volumeId:guid}")] HttpRequest request,
        Guid volumeId,
        FunctionContext executionContext)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(executionContext);

        _logger.LogInformation("Removing purchase for {UserId} of {VolumeId}", _currentUser.Id, volumeId);

        var result = await _removeUseCase.ExecuteAsync(
            new RemovePurchaseCommand(volumeId),
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

/// <summary>POST <c>/api/me/purchases</c> request body.</summary>
public sealed record AddPurchaseRequest(Guid VolumeId, DateTime? PurchasedAt);
