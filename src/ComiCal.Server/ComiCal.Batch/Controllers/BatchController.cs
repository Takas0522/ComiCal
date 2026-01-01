using System;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using ComiCal.Batch.Models;
using ComiCal.Batch.Services;
using ComiCal.Batch.Util;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ComiCal.Batch.Controllers
{
    /// <summary>
    /// API Controller for manual execution of batch jobs
    /// Provides endpoints for triggering jobs, partial retries, and intervention management
    /// </summary>
    public class BatchController
    {
        private readonly IJobTriggerService _jobTriggerService;
        private readonly JobSchedulingService _jobSchedulingService;
        private readonly IBatchStateService _batchStateService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<BatchController> _logger;

        public BatchController(
            IJobTriggerService jobTriggerService,
            JobSchedulingService jobSchedulingService,
            IBatchStateService batchStateService,
            IConfiguration configuration,
            ILogger<BatchController> logger)
        {
            _jobTriggerService = jobTriggerService;
            _jobSchedulingService = jobSchedulingService;
            _batchStateService = batchStateService;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// POST /api/batch/registration
        /// Manually trigger data registration job
        /// </summary>
        [Function("TriggerRegistrationJob")]
        public async Task<HttpResponseData> TriggerRegistrationJob(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "batch/registration")] HttpRequestData req)
        {
            _logger.LogInformation("TriggerRegistrationJob endpoint called");

            // Authenticate API key
            if (!ApiKeyAuthenticationHelper.ValidateApiKey(req, _configuration, _logger))
            {
                return await ApiKeyAuthenticationHelper.CreateUnauthorizedResponseAsync(req);
            }

            try
            {
                var (success, message, batchId) = await _jobTriggerService.TriggerRegistrationJobAsync();

                var responseData = new JobExecutionResponse
                {
                    Success = success,
                    Message = message,
                    BatchId = batchId,
                    JobType = "DataRegistration",
                    Timestamp = DateTime.UtcNow
                };

                var response = req.CreateResponse(success ? HttpStatusCode.OK : HttpStatusCode.BadRequest);
                await response.WriteAsJsonAsync(responseData);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in TriggerRegistrationJob endpoint");
                return await CreateErrorResponseAsync(req, ex);
            }
        }

        /// <summary>
        /// POST /api/batch/images
        /// Manually trigger image download job
        /// </summary>
        [Function("TriggerImageDownloadJob")]
        public async Task<HttpResponseData> TriggerImageDownloadJob(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "batch/images")] HttpRequestData req)
        {
            _logger.LogInformation("TriggerImageDownloadJob endpoint called");

            // Authenticate API key
            if (!ApiKeyAuthenticationHelper.ValidateApiKey(req, _configuration, _logger))
            {
                return await ApiKeyAuthenticationHelper.CreateUnauthorizedResponseAsync(req);
            }

            try
            {
                var (success, message, batchId) = await _jobTriggerService.TriggerImageDownloadJobAsync();

                var responseData = new JobExecutionResponse
                {
                    Success = success,
                    Message = message,
                    BatchId = batchId,
                    JobType = "ImageDownload",
                    Timestamp = DateTime.UtcNow
                };

                var response = req.CreateResponse(success ? HttpStatusCode.OK : HttpStatusCode.BadRequest);
                await response.WriteAsJsonAsync(responseData);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in TriggerImageDownloadJob endpoint");
                return await CreateErrorResponseAsync(req, ex);
            }
        }

        /// <summary>
        /// POST /api/batch/registration/partial?startPage={}&amp;endPage={}
        /// Trigger partial retry for specific page range
        /// </summary>
        [Function("TriggerPartialRetry")]
        public async Task<HttpResponseData> TriggerPartialRetry(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "batch/registration/partial")] HttpRequestData req)
        {
            _logger.LogInformation("TriggerPartialRetry endpoint called");

            // Authenticate API key
            if (!ApiKeyAuthenticationHelper.ValidateApiKey(req, _configuration, _logger))
            {
                return await ApiKeyAuthenticationHelper.CreateUnauthorizedResponseAsync(req);
            }

            try
            {
                // Parse query parameters
                var queryParams = HttpUtility.ParseQueryString(req.Url.Query);
                var startPageStr = queryParams["startPage"];
                var endPageStr = queryParams["endPage"];

                if (string.IsNullOrWhiteSpace(startPageStr) || string.IsNullOrWhiteSpace(endPageStr))
                {
                    var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await errorResponse.WriteAsJsonAsync(new ErrorResponse
                    {
                        Error = "Missing required parameters",
                        Details = "Both startPage and endPage query parameters are required",
                        Timestamp = DateTime.UtcNow
                    });
                    return errorResponse;
                }

                if (!int.TryParse(startPageStr, out int startPage) || !int.TryParse(endPageStr, out int endPage))
                {
                    var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await errorResponse.WriteAsJsonAsync(new ErrorResponse
                    {
                        Error = "Invalid parameter format",
                        Details = "startPage and endPage must be valid integers",
                        Timestamp = DateTime.UtcNow
                    });
                    return errorResponse;
                }

                var (success, message, batchId, pageCount) = await _jobTriggerService.TriggerPartialRetryAsync(
                    startPage,
                    endPage);

                var responseData = new PartialRetryResponse
                {
                    Success = success,
                    Message = message,
                    BatchId = batchId,
                    StartPage = startPage,
                    EndPage = endPage,
                    PageCount = pageCount,
                    Timestamp = DateTime.UtcNow
                };

                var response = req.CreateResponse(success ? HttpStatusCode.OK : HttpStatusCode.BadRequest);
                await response.WriteAsJsonAsync(responseData);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in TriggerPartialRetry endpoint");
                return await CreateErrorResponseAsync(req, ex);
            }
        }

        /// <summary>
        /// POST /api/batch/reset-intervention
        /// Clear manual intervention flag and enable auto-resume
        /// </summary>
        [Function("ResetIntervention")]
        public async Task<HttpResponseData> ResetIntervention(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "batch/reset-intervention")] HttpRequestData req)
        {
            _logger.LogInformation("ResetIntervention endpoint called");

            // Authenticate API key
            if (!ApiKeyAuthenticationHelper.ValidateApiKey(req, _configuration, _logger))
            {
                return await ApiKeyAuthenticationHelper.CreateUnauthorizedResponseAsync(req);
            }

            try
            {
                // Try to get batch ID from query parameter or use today's batch
                var queryParams = HttpUtility.ParseQueryString(req.Url.Query);
                var batchIdStr = queryParams["batchId"];

                int? batchId = null;
                if (!string.IsNullOrWhiteSpace(batchIdStr) && int.TryParse(batchIdStr, out int parsedBatchId))
                {
                    batchId = parsedBatchId;
                }

                // If no batch ID provided, use today's batch
                if (!batchId.HasValue)
                {
                    var batchDate = DateTime.UtcNow.Date;
                    var batchState = await _batchStateService.GetBatchStateByDateAsync(batchDate);

                    if (batchState == null)
                    {
                        var errorResponse = req.CreateResponse(HttpStatusCode.NotFound);
                        await errorResponse.WriteAsJsonAsync(new ErrorResponse
                        {
                            Error = "Batch not found",
                            Details = "No batch found for today. Provide a specific batchId query parameter.",
                            Timestamp = DateTime.UtcNow
                        });
                        return errorResponse;
                    }

                    batchId = batchState.Id;
                }

                // Clear manual intervention
                await _jobSchedulingService.ClearManualInterventionAsync(batchId.Value);

                var responseData = new ResetInterventionResponse
                {
                    Success = true,
                    Message = $"Manual intervention cleared for batch {batchId}. Job will auto-resume on next scheduled run.",
                    BatchId = batchId.Value,
                    Timestamp = DateTime.UtcNow
                };

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(responseData);
                return response;
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation in ResetIntervention endpoint");
                var errorResponse = req.CreateResponse(HttpStatusCode.NotFound);
                await errorResponse.WriteAsJsonAsync(new ErrorResponse
                {
                    Error = "Not found",
                    Details = ex.Message,
                    Timestamp = DateTime.UtcNow
                });
                return errorResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ResetIntervention endpoint");
                return await CreateErrorResponseAsync(req, ex);
            }
        }

        /// <summary>
        /// Create error response for unhandled exceptions
        /// </summary>
        private async Task<HttpResponseData> CreateErrorResponseAsync(HttpRequestData req, Exception ex)
        {
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteAsJsonAsync(new ErrorResponse
            {
                Error = "Internal server error",
                Details = ex.Message,
                Timestamp = DateTime.UtcNow
            });
            return response;
        }
    }
}
