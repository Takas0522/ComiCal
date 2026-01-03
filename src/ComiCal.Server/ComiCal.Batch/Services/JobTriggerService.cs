using System;
using System.Threading;
using System.Threading.Tasks;
using ComiCal.Batch.Jobs;
using ComiCal.Shared.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ComiCal.Batch.Services
{
    /// <summary>
    /// Service for manually triggering batch jobs
    /// </summary>
    public interface IJobTriggerService
    {
        /// <summary>
        /// Trigger registration job manually
        /// </summary>
        Task<(bool Success, string Message, int? BatchId)> TriggerRegistrationJobAsync();

        /// <summary>
        /// Trigger image download job manually
        /// </summary>
        Task<(bool Success, string Message, int? BatchId)> TriggerImageDownloadJobAsync();

        /// <summary>
        /// Trigger partial retry for specific page range
        /// </summary>
        Task<(bool Success, string Message, int? BatchId, int PageCount)> TriggerPartialRetryAsync(
            int startPage, 
            int endPage);
    }

    /// <summary>
    /// Implementation of job trigger service
    /// </summary>
    public class JobTriggerService : IJobTriggerService
    {
        private readonly IComicService _comicService;
        private readonly IBatchStateService _batchStateService;
        private readonly JobSchedulingService _jobSchedulingService;
        private readonly PartialRetryService _partialRetryService;
        private readonly ILogger<JobTriggerService> _logger;

        public JobTriggerService(
            IComicService comicService,
            IBatchStateService batchStateService,
            JobSchedulingService jobSchedulingService,
            PartialRetryService partialRetryService,
            ILogger<JobTriggerService> logger)
        {
            _comicService = comicService;
            _batchStateService = batchStateService;
            _jobSchedulingService = jobSchedulingService;
            _partialRetryService = partialRetryService;
            _logger = logger;
        }

        /// <summary>
        /// Trigger registration job manually
        /// </summary>
        public async Task<(bool Success, string Message, int? BatchId)> TriggerRegistrationJobAsync()
        {
            try
            {
                _logger.LogInformation("Manual registration job trigger requested");

                // Get or create batch state for today
                var batchDate = DateTime.UtcNow.Date;
                var batchState = await _batchStateService.GetOrCreateBatchStateAsync(batchDate);

                _logger.LogInformation(
                    "Batch state for manual registration. BatchId: {BatchId}, Status: {Status}, RegistrationPhase: {Phase}",
                    batchState.Id, batchState.Status, batchState.RegistrationPhase);

                // Check if job can proceed
                var (canProceed, reason) = await _jobSchedulingService.CanJobProceedAsync(
                    batchState.Id,
                    BatchPhase.Registration);

                if (!canProceed)
                {
                    _logger.LogWarning(
                        "Manual registration job cannot proceed. BatchId: {BatchId}, Reason: {Reason}",
                        batchState.Id, reason);
                    return (false, $"Job cannot proceed: {reason}", batchState.Id);
                }

                // Check if already completed
                if (batchState.RegistrationPhase == PhaseStatus.Completed)
                {
                    _logger.LogInformation(
                        "Registration phase already completed for batch {BatchId}",
                        batchState.Id);
                    return (true, "Registration phase already completed", batchState.Id);
                }

                // Update status to running
                await _batchStateService.UpdateBatchStatusAsync(batchState.Id, BatchStatus.Running);
                await _batchStateService.UpdatePhaseStatusAsync(
                    batchState.Id,
                    BatchPhase.Registration,
                    PhaseStatus.Running);

                _logger.LogInformation(
                    "Manual registration job triggered successfully. BatchId: {BatchId}",
                    batchState.Id);

                return (true, "Registration job triggered successfully. Job is now running in the background.", batchState.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error triggering manual registration job");
                return (false, $"Error: {ex.Message}", null);
            }
        }

        /// <summary>
        /// Trigger image download job manually
        /// </summary>
        public async Task<(bool Success, string Message, int? BatchId)> TriggerImageDownloadJobAsync()
        {
            try
            {
                _logger.LogInformation("Manual image download job trigger requested");

                // Get or create batch state for today
                var batchDate = DateTime.UtcNow.Date;
                var batchState = await _batchStateService.GetOrCreateBatchStateAsync(batchDate);

                _logger.LogInformation(
                    "Batch state for manual image download. BatchId: {BatchId}, Status: {Status}, ImageDownloadPhase: {Phase}",
                    batchState.Id, batchState.Status, batchState.ImageDownloadPhase);

                // Check if job can proceed (includes dependency check on registration phase)
                var (canProceed, reason) = await _jobSchedulingService.CanJobProceedAsync(
                    batchState.Id,
                    BatchPhase.ImageDownload);

                if (!canProceed)
                {
                    _logger.LogWarning(
                        "Manual image download job cannot proceed. BatchId: {BatchId}, Reason: {Reason}",
                        batchState.Id, reason);
                    return (false, $"Job cannot proceed: {reason}", batchState.Id);
                }

                // Check if already completed
                if (batchState.ImageDownloadPhase == PhaseStatus.Completed)
                {
                    _logger.LogInformation(
                        "Image download phase already completed for batch {BatchId}",
                        batchState.Id);
                    return (true, "Image download phase already completed", batchState.Id);
                }

                // Update status to running
                await _batchStateService.UpdateBatchStatusAsync(batchState.Id, BatchStatus.Running);
                await _batchStateService.UpdatePhaseStatusAsync(
                    batchState.Id,
                    BatchPhase.ImageDownload,
                    PhaseStatus.Running);

                _logger.LogInformation(
                    "Manual image download job triggered successfully. BatchId: {BatchId}",
                    batchState.Id);

                return (true, "Image download job triggered successfully. Job is now running in the background.", batchState.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error triggering manual image download job");
                return (false, $"Error: {ex.Message}", null);
            }
        }

        /// <summary>
        /// Trigger partial retry for specific page range
        /// </summary>
        public async Task<(bool Success, string Message, int? BatchId, int PageCount)> TriggerPartialRetryAsync(
            int startPage,
            int endPage)
        {
            try
            {
                _logger.LogInformation(
                    "Manual partial retry trigger requested. StartPage: {StartPage}, EndPage: {EndPage}",
                    startPage, endPage);

                // Validate page range
                if (startPage < 1 || endPage < startPage)
                {
                    return (false, $"Invalid page range: {startPage}-{endPage}. Start page must be >= 1 and end page must be >= start page.", null, 0);
                }

                // Get batch state for today
                var batchDate = DateTime.UtcNow.Date;
                var batchState = await _batchStateService.GetBatchStateByDateAsync(batchDate);

                if (batchState == null)
                {
                    return (false, "No batch state found for today. Run a full job first.", null, 0);
                }

                _logger.LogInformation(
                    "Batch state for partial retry. BatchId: {BatchId}, Status: {Status}",
                    batchState.Id, batchState.Status);

                // Reset page range for retry
                await _partialRetryService.ResetPageRangeAsync(
                    batchState.Id,
                    startPage,
                    endPage,
                    BatchPhase.Registration);

                // Calculate page count
                int pageCount = endPage - startPage + 1;

                // Update batch status to allow retry
                await _batchStateService.UpdatePhaseStatusAsync(
                    batchState.Id,
                    BatchPhase.Registration,
                    PhaseStatus.Running);

                _logger.LogInformation(
                    "Partial retry triggered successfully. BatchId: {BatchId}, Pages: {StartPage}-{EndPage}, Count: {PageCount}",
                    batchState.Id, startPage, endPage, pageCount);

                return (true,
                    $"Partial retry triggered for pages {startPage}-{endPage} ({pageCount} pages). Job is now running in the background.",
                    batchState.Id,
                    pageCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error triggering partial retry. StartPage: {StartPage}, EndPage: {EndPage}",
                    startPage, endPage);
                return (false, $"Error: {ex.Message}", null, 0);
            }
        }
    }
}
