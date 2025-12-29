using System;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Comical.Api.Util.Common
{
    /// <summary>
    /// Helper class for executing Azure Function logic with standardized error handling.
    /// </summary>
    public static class FunctionExecutionHelper
    {
        /// <summary>
        /// Executes a function with try-catch error handling, converting exceptions to appropriate HTTP responses.
        /// </summary>
        /// <param name="request">The HTTP request data.</param>
        /// <param name="logger">The logger instance.</param>
        /// <param name="action">The async function to execute.</param>
        /// <returns>An HTTP response - either the result of the action or an error response.</returns>
        public static async Task<HttpResponseData> ExecuteAsync(
            HttpRequestData request,
            ILogger logger,
            Func<Task<HttpResponseData>> action)
        {
            try
            {
                return await action();
            }
            catch (InvalidOperationException ex)
            {
                logger.LogWarning(ex, "Invalid operation: {Message}", ex.Message);
                return await HttpResponseHelper.CreateBadRequestResponseAsync(
                    request,
                    "Invalid operation",
                    ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error occurred");
                return await HttpResponseHelper.CreateErrorResponseAsync(
                    request,
                    "Internal server error",
                    null);
            }
        }
    }
}
