using System.Net;
using System.Threading.Tasks;
using ComiCal.Api.Common;
using ComiCal.Api.Middleware;
using ComiCal.Api.ProblemDetails;
using ComiCal.Application.Common;
using ComiCal.Application.DTOs;
using ComiCal.Application.UseCases;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.Logging;

namespace ComiCal.Api.Functions;

/// <summary>HTTP triggers for the <c>/api/volumes</c> resource (search + ISBN lookup).</summary>
public sealed class VolumeFunctions(
    ILogger<VolumeFunctions> logger,
    ISearchVolumesUseCase searchUseCase,
    IGetVolumeByIsbnUseCase byIsbnUseCase,
    IValidator<SearchVolumesQuery> searchValidator,
    IValidator<GetVolumeByIsbnQuery> byIsbnValidator,
    ProblemDetailsFactory problemDetailsFactory)
{
    private readonly ILogger<VolumeFunctions> _logger = logger;
    private readonly ISearchVolumesUseCase _searchUseCase = searchUseCase;
    private readonly IGetVolumeByIsbnUseCase _byIsbnUseCase = byIsbnUseCase;
    private readonly IValidator<SearchVolumesQuery> _searchValidator = searchValidator;
    private readonly IValidator<GetVolumeByIsbnQuery> _byIsbnValidator = byIsbnValidator;
    private readonly ProblemDetailsFactory _problemDetailsFactory = problemDetailsFactory;

    [Function("SearchVolumes")]
    [OpenApiOperation(operationId: "SearchVolumes", tags: ["Volumes"], Summary = "Search volumes", Description = "Keyset-paginated search for individual volumes by title and release window.")]
    [OpenApiParameter(name: "q", In = ParameterLocation.Query, Required = false, Type = typeof(string), Description = "Free-text search term.")]
    [OpenApiParameter(name: "releaseFrom", In = ParameterLocation.Query, Required = false, Type = typeof(DateOnly), Description = "Inclusive ISO date lower bound.")]
    [OpenApiParameter(name: "releaseTo", In = ParameterLocation.Query, Required = false, Type = typeof(DateOnly), Description = "Inclusive ISO date upper bound.")]
    [OpenApiParameter(name: "limit", In = ParameterLocation.Query, Required = false, Type = typeof(int), Description = "Page size (1–100, default 20).")]
    [OpenApiParameter(name: "cursor", In = ParameterLocation.Query, Required = false, Type = typeof(string), Description = "Opaque keyset cursor.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(VolumeSearchResultDto), Summary = "Matching volumes.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NotModified, Summary = "ETag matched If-None-Match.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/problem+json", bodyType: typeof(ProblemDetailsBody), Summary = "Invalid query parameters.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.TooManyRequests, contentType: "application/problem+json", bodyType: typeof(ProblemDetailsBody), Summary = "Rate limit exceeded.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.InternalServerError, contentType: "application/problem+json", bodyType: typeof(ProblemDetailsBody), Summary = "Unexpected error.")]
    public async Task<IActionResult> SearchAsync(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "volumes")] HttpRequest request,
        FunctionContext executionContext)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(executionContext);

        var query = new SearchVolumesQuery(
            Query: QueryParameters.GetString(request, "q"),
            ReleaseFrom: QueryParameters.GetDate(request, "releaseFrom"),
            ReleaseTo: QueryParameters.GetDate(request, "releaseTo"),
            Limit: QueryParameters.GetInt(request, "limit", defaultValue: 20),
            Cursor: QueryParameters.GetString(request, "cursor"));

        var validation = await _searchValidator.ValidateAsync(query, request.HttpContext.RequestAborted).ConfigureAwait(false);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.Errors);
        }

        _logger.LogInformation("Searching volumes {@Query}", query);
        var result = await _searchUseCase.ExecuteAsync(
            query,
            new UseCaseContext(UserId: null, CorrelationId: CorrelationContextAccessor.GetCorrelationId(executionContext)),
            request.HttpContext.RequestAborted).ConfigureAwait(false);

        return await UseCaseResultMapper.ToActionResultAsync(result, request, executionContext, _problemDetailsFactory, useEtag: true, cacheControl: CacheControlPolicies.AnonymousCatalog).ConfigureAwait(false);
    }

    // ETag is intentionally NOT applied to /volumes/by-isbn/{isbn}: it returns a single
    // resource looked up by a unique key, so the cache-validation round-trip provides
    // negligible value relative to a plain 200/404 response.
    [Function("GetVolumeByIsbn")]
    [OpenApiOperation(operationId: "GetVolumeByIsbn", tags: ["Volumes"], Summary = "Get volume by ISBN-13", Description = "Looks up a single volume by its ISBN-13.")]
    [OpenApiParameter(name: "isbn", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "ISBN-13 (13 digits, no hyphens).")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(VolumeDto), Summary = "The volume.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/problem+json", bodyType: typeof(ProblemDetailsBody), Summary = "Malformed ISBN.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "application/problem+json", bodyType: typeof(ProblemDetailsBody), Summary = "No volume with that ISBN.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.TooManyRequests, contentType: "application/problem+json", bodyType: typeof(ProblemDetailsBody), Summary = "Rate limit exceeded.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.InternalServerError, contentType: "application/problem+json", bodyType: typeof(ProblemDetailsBody), Summary = "Unexpected error.")]
    public async Task<IActionResult> GetByIsbnAsync(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "volumes/by-isbn/{isbn}")] HttpRequest request,
        string isbn,
        FunctionContext executionContext)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(executionContext);

        var query = new GetVolumeByIsbnQuery(Isbn: isbn);
        var validation = await _byIsbnValidator.ValidateAsync(query, request.HttpContext.RequestAborted).ConfigureAwait(false);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.Errors);
        }

        _logger.LogInformation("Looking up volume by isbn {Isbn}", isbn);
        var result = await _byIsbnUseCase.ExecuteAsync(
            query,
            new UseCaseContext(UserId: null, CorrelationId: CorrelationContextAccessor.GetCorrelationId(executionContext)),
            request.HttpContext.RequestAborted).ConfigureAwait(false);

        return await UseCaseResultMapper.ToActionResultAsync(result, request, executionContext, _problemDetailsFactory, useEtag: false).ConfigureAwait(false);
    }
}
