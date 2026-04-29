using System.Net;
using System.Threading.Tasks;
using ComiCal.Api.Common;
using ComiCal.Api.Middleware;
using ComiCal.Api.ProblemDetails;
using ComiCal.Application.Common;
using ComiCal.Application.DTOs;
using ComiCal.Application.UseCases;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.Logging;

namespace ComiCal.Api.Functions;

/// <summary>HTTP trigger for the <c>/api/calendar</c> resource.</summary>
public sealed class CalendarFunction(
    ILogger<CalendarFunction> logger,
    IGetCalendarUseCase useCase,
    IValidator<GetCalendarQuery> validator,
    ProblemDetailsFactory problemDetailsFactory)
{
    private readonly ILogger<CalendarFunction> _logger = logger;
    private readonly IGetCalendarUseCase _useCase = useCase;
    private readonly IValidator<GetCalendarQuery> _validator = validator;
    private readonly ProblemDetailsFactory _problemDetailsFactory = problemDetailsFactory;

    [Function("GetCalendar")]
    [OpenApiOperation(operationId: "GetCalendar", tags: ["Calendar"], Summary = "Get release calendar", Description = "Returns release dates grouped by day for N consecutive months starting at monthFrom.")]
    [OpenApiParameter(name: "monthFrom", In = ParameterLocation.Query, Required = true, Type = typeof(string), Description = "Start month in ISO yyyy-MM form.")]
    [OpenApiParameter(name: "monthCount", In = ParameterLocation.Query, Required = false, Type = typeof(int), Description = "Number of months (1–12, default 3).")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(CalendarDto), Summary = "Calendar payload.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NotModified, Summary = "ETag matched If-None-Match.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/problem+json", bodyType: typeof(ProblemDetailsBody), Summary = "Invalid query parameters.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.TooManyRequests, contentType: "application/problem+json", bodyType: typeof(ProblemDetailsBody), Summary = "Rate limit exceeded.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.InternalServerError, contentType: "application/problem+json", bodyType: typeof(ProblemDetailsBody), Summary = "Unexpected error.")]
    public async Task<IActionResult> RunAsync(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "calendar")] HttpRequest request,
        FunctionContext executionContext)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(executionContext);

        var monthFrom = QueryParameters.GetMonth(request, "monthFrom");
        if (monthFrom is null)
        {
            throw new ValidationException(new[] { new ValidationFailure("monthFrom", "Query parameter 'monthFrom' is required (yyyy-MM).") });
        }

        var query = new GetCalendarQuery(
            MonthFrom: monthFrom.Value,
            MonthCount: QueryParameters.GetInt(request, "monthCount", defaultValue: 3));

        var validation = await _validator.ValidateAsync(query, request.HttpContext.RequestAborted).ConfigureAwait(false);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.Errors);
        }

        _logger.LogInformation("Fetching calendar {@Query}", query);
        var result = await _useCase.ExecuteAsync(
            query,
            new UseCaseContext(UserId: null, CorrelationId: CorrelationContextAccessor.GetCorrelationId(executionContext)),
            request.HttpContext.RequestAborted).ConfigureAwait(false);

        return await UseCaseResultMapper.ToActionResultAsync(result, request, executionContext, _problemDetailsFactory, useEtag: true, cacheControl: CacheControlPolicies.AnonymousCatalog).ConfigureAwait(false);
    }
}
