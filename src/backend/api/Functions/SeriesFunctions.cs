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

/// <summary>HTTP triggers for the <c>/api/series</c> resource (search + detail).</summary>
public sealed class SeriesFunctions(
    ILogger<SeriesFunctions> logger,
    ISearchSeriesUseCase searchUseCase,
    IGetSeriesDetailUseCase detailUseCase,
    IValidator<SearchSeriesQuery> searchValidator,
    IValidator<GetSeriesDetailQuery> detailValidator,
    ProblemDetailsFactory problemDetailsFactory)
{
    private readonly ILogger<SeriesFunctions> _logger = logger;
    private readonly ISearchSeriesUseCase _searchUseCase = searchUseCase;
    private readonly IGetSeriesDetailUseCase _detailUseCase = detailUseCase;
    private readonly IValidator<SearchSeriesQuery> _searchValidator = searchValidator;
    private readonly IValidator<GetSeriesDetailQuery> _detailValidator = detailValidator;
    private readonly ProblemDetailsFactory _problemDetailsFactory = problemDetailsFactory;

    [Function("SearchSeries")]
    [OpenApiOperation(operationId: "SearchSeries", tags: ["Series"], Summary = "Search series", Description = "Keyset-paginated full-text search for manga series.")]
    [OpenApiParameter(name: "q", In = ParameterLocation.Query, Required = false, Type = typeof(string), Description = "Free-text search term (hiragana-normalised server-side).")]
    [OpenApiParameter(name: "publisherId", In = ParameterLocation.Query, Required = false, Type = typeof(Guid), Description = "Filter by publisher id.")]
    [OpenApiParameter(name: "authorId", In = ParameterLocation.Query, Required = false, Type = typeof(Guid), Description = "Filter by author id.")]
    [OpenApiParameter(name: "limit", In = ParameterLocation.Query, Required = false, Type = typeof(int), Description = "Page size (1–100, default 20).")]
    [OpenApiParameter(name: "cursor", In = ParameterLocation.Query, Required = false, Type = typeof(Guid), Description = "Keyset cursor returned by the previous page.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(SeriesSearchResultDto), Summary = "Matching series.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NotModified, Summary = "ETag matched If-None-Match.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/problem+json", bodyType: typeof(ProblemDetailsBody), Summary = "Invalid query parameters.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.TooManyRequests, contentType: "application/problem+json", bodyType: typeof(ProblemDetailsBody), Summary = "Rate limit exceeded.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.InternalServerError, contentType: "application/problem+json", bodyType: typeof(ProblemDetailsBody), Summary = "Unexpected error.")]
    public async Task<IActionResult> SearchAsync(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "series")] HttpRequest request,
        FunctionContext executionContext)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(executionContext);

        var query = new SearchSeriesQuery(
            Query: QueryParameters.GetString(request, "q"),
            PublisherId: QueryParameters.GetGuid(request, "publisherId"),
            AuthorId: QueryParameters.GetGuid(request, "authorId"),
            Limit: QueryParameters.GetInt(request, "limit", defaultValue: 20),
            Cursor: QueryParameters.GetGuid(request, "cursor"));

        await ValidateAsync(_searchValidator, query, request.HttpContext.RequestAborted).ConfigureAwait(false);

        _logger.LogInformation("Searching series {@Query}", query);
        var result = await _searchUseCase.ExecuteAsync(
            query,
            new UseCaseContext(UserId: null, CorrelationId: CorrelationContextAccessor.GetCorrelationId(executionContext)),
            request.HttpContext.RequestAborted).ConfigureAwait(false);

        return await UseCaseResultMapper.ToActionResultAsync(result, request, executionContext, _problemDetailsFactory, useEtag: true, cacheControl: CacheControlPolicies.AnonymousCatalog).ConfigureAwait(false);
    }

    [Function("GetSeriesDetail")]
    [OpenApiOperation(operationId: "GetSeriesDetail", tags: ["Series"], Summary = "Get series detail", Description = "Returns a series with its volumes (optionally filtered by release date).")]
    [OpenApiParameter(name: "id", In = ParameterLocation.Path, Required = true, Type = typeof(Guid), Description = "Series identifier (GUID).")]
    [OpenApiParameter(name: "releaseFrom", In = ParameterLocation.Query, Required = false, Type = typeof(DateOnly), Description = "Filter volumes whose release date is on or after this ISO date.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(SeriesDetailDto), Summary = "Series with volumes.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NotModified, Summary = "ETag matched If-None-Match.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/problem+json", bodyType: typeof(ProblemDetailsBody), Summary = "Invalid path or query parameter.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "application/problem+json", bodyType: typeof(ProblemDetailsBody), Summary = "Series not found.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.TooManyRequests, contentType: "application/problem+json", bodyType: typeof(ProblemDetailsBody), Summary = "Rate limit exceeded.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.InternalServerError, contentType: "application/problem+json", bodyType: typeof(ProblemDetailsBody), Summary = "Unexpected error.")]
    public async Task<IActionResult> GetDetailAsync(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "series/{id:guid}")] HttpRequest request,
        Guid id,
        FunctionContext executionContext)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(executionContext);

        var query = new GetSeriesDetailQuery(
            SeriesId: id,
            ReleaseFrom: QueryParameters.GetDate(request, "releaseFrom"));

        await ValidateAsync(_detailValidator, query, request.HttpContext.RequestAborted).ConfigureAwait(false);

        _logger.LogInformation("Fetching series detail {SeriesId} releaseFrom={ReleaseFrom}", id, query.ReleaseFrom);
        var result = await _detailUseCase.ExecuteAsync(
            query,
            new UseCaseContext(UserId: null, CorrelationId: CorrelationContextAccessor.GetCorrelationId(executionContext)),
            request.HttpContext.RequestAborted).ConfigureAwait(false);

        return await UseCaseResultMapper.ToActionResultAsync(result, request, executionContext, _problemDetailsFactory, useEtag: true, cacheControl: CacheControlPolicies.AnonymousCatalog).ConfigureAwait(false);
    }

    private static async Task ValidateAsync<T>(IValidator<T> validator, T instance, CancellationToken ct)
    {
        var result = await validator.ValidateAsync(instance, ct).ConfigureAwait(false);
        if (!result.IsValid)
        {
            throw new ValidationException(result.Errors);
        }
    }
}
