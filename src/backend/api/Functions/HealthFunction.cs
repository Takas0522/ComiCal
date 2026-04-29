using ComiCal.Application.Common;
using ComiCal.Application.UseCases;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;

namespace ComiCal.Api.Functions;

public sealed class HealthFunction(ILogger<HealthFunction> logger, IGetHealthUseCase useCase)
{
    private readonly ILogger<HealthFunction> _logger = logger;
    private readonly IGetHealthUseCase _useCase = useCase;

    [Function("Health")]
    [OpenApiOperation(operationId: "getHealth", tags: ["health"], Summary = "Health probe", Description = "Returns service liveness state.")]
    [OpenApiResponseWithBody(statusCode: System.Net.HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(HealthStatus), Summary = "OK")]
    public async System.Threading.Tasks.Task<IActionResult> RunAsync(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "health")] HttpRequest request,
        FunctionContext executionContext)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(executionContext);
        _logger.LogInformation("Health probe invoked {InvocationId}", executionContext.InvocationId);

        var result = await _useCase.ExecuteAsync(
            new GetHealthQuery(),
            new UseCaseContext(UserId: null, CorrelationId: executionContext.InvocationId),
            request.HttpContext.RequestAborted).ConfigureAwait(false);

        return new OkObjectResult(result.Value);
    }
}
