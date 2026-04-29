using System.Net;
using System.Threading.Tasks;
using ComiCal.Api.Common;
using ComiCal.Api.Middleware;
using ComiCal.Api.ProblemDetails;
using ComiCal.Application.Common;
using ComiCal.Application.UseCases.Me;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;

namespace ComiCal.Api.Functions;

/// <summary>
/// HTTP triggers for <c>/api/me/account</c> (Phase 2 self-service account
/// management). The user identifier is read from <see cref="ICurrentUser.Id"/>;
/// the endpoint takes no request body. Account deletion is a HARD delete
/// (個人情報保護法準拠) that physically removes the <c>Users</c> row and all
/// FK-related rows in a single transaction.
/// </summary>
[RequiresAuthenticatedUser]
public sealed class MeAccountFunctions(
    ILogger<MeAccountFunctions> logger,
    IDeleteAccountUseCase deleteAccountUseCase,
    ICurrentUser currentUser,
    ProblemDetailsFactory problemDetailsFactory)
{
    /// <summary>
    /// Response header set on a successful 204 to instruct the SWA frontend to
    /// drive the user through <c>/.auth/logout</c>: the cookie may still be
    /// valid for a few minutes but the principal no longer maps to a user row.
    /// </summary>
    public const string LogoutRequiredHeader = "X-Logout-Required";

    private readonly ILogger<MeAccountFunctions> _logger = logger;
    private readonly IDeleteAccountUseCase _deleteAccountUseCase = deleteAccountUseCase;
    private readonly ICurrentUser _currentUser = currentUser;
    private readonly ProblemDetailsFactory _problemDetailsFactory = problemDetailsFactory;

    [Function("DeleteMeAccount")]
    [OpenApiOperation(operationId: "DeleteMeAccount", tags: ["Me", "Account"], Summary = "Delete my account", Description = "Hard-deletes the authenticated user and all FK-owned rows (Subscriptions, Purchases, IdentityLinks). Idempotent. Sets X-Logout-Required: true so the client can redirect through /.auth/logout.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NoContent, Summary = "Account deleted (or already absent). X-Logout-Required: true.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Unauthorized, contentType: "application/problem+json", bodyType: typeof(ProblemDetailsBody), Summary = "Authentication required.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.TooManyRequests, contentType: "application/problem+json", bodyType: typeof(ProblemDetailsBody), Summary = "Rate limit exceeded.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.InternalServerError, contentType: "application/problem+json", bodyType: typeof(ProblemDetailsBody), Summary = "Unexpected error.")]
    public async Task<IActionResult> DeleteAsync(
        [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "me/account")] HttpRequest request,
        FunctionContext executionContext)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(executionContext);

        var userId = _currentUser.Id;
        _logger.LogInformation("Hard-deleting account for {UserId}", userId);

        var result = await _deleteAccountUseCase.ExecuteAsync(
            new DeleteAccountCommand(userId),
            BuildContext(executionContext),
            request.HttpContext.RequestAborted).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return UseCaseResultMapper.ToProblem(result.Error, request, executionContext, _problemDetailsFactory);
        }

        // Signal to the frontend that the SWA cookie is now stale and a logout
        // round-trip is required. NoContentResult sets the 204 status; we attach
        // the header directly via the response object.
        request.HttpContext.Response.Headers[LogoutRequiredHeader] = "true";
        return new NoContentResult();
    }

    private UseCaseContext BuildContext(FunctionContext executionContext)
        => new(
            UserId: _currentUser.IsAuthenticated ? _currentUser.Id : null,
            CorrelationId: CorrelationContextAccessor.GetCorrelationId(executionContext) ?? executionContext.InvocationId);
}
