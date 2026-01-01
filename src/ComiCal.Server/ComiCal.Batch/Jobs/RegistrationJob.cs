using System;
using System.Threading;
using System.Threading.Tasks;
using ComiCal.Batch.Services;
using ComiCal.Shared.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ComiCal.Batch.Jobs
{
    /// <summary>
    /// Container Job for comic data registration from Rakuten Books API
    /// Implements checkpoint-based processing with 120-second rate limiting
    /// </summary>
    public class RegistrationJob : BackgroundService
    {
        private readonly IComicService _comicService;
        private readonly IBatchStateService _batchStateService;
        private readonly JobSchedulingService _jobSchedulingService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<RegistrationJob> _logger;
        private readonly IHostApplicationLifetime _applicationLifetime;
        
        // Rate limiting: 120 seconds between API calls as per Rakuten API requirements
        private const int RateLimitDelaySeconds = 120;
        
        // Job type identifier from environment variable
        private const string JobTypeEnvironmentVariable = "BATCH_JOB_TYPE";
        private const string ExpectedJobType = "DataRegistration";

        public RegistrationJob(
            IComicService comicService,
            IBatchStateService batchStateService,
            JobSchedulingService jobSchedulingService,
            IConfiguration configuration,
            ILogger<RegistrationJob> logger,
            IHostApplicationLifetime applicationLifetime)
        {
            _comicService = comicService;
            _batchStateService = batchStateService;
            _jobSchedulingService = jobSchedulingService;
            _configuration = configuration;
            _logger = logger;
            _applicationLifetime = applicationLifetime;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                // Check if this job should run based on environment variable
                var jobType = _configuration[JobTypeEnvironmentVariable];
                if (string.IsNullOrWhiteSpace(jobType) || !jobType.Equals(ExpectedJobType, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation(
                        "Job type mismatch. Expected: {ExpectedJobType}, Actual: {ActualJobType}. Skipping execution.",
                        ExpectedJobType, jobType ?? "null");
                    return;
                }

                _logger.LogInformation("Starting Data Registration Job for {ExpectedJobType}", ExpectedJobType);

                // Get or create batch state for today
                var batchDate = DateTime.UtcNow.Date;
                var batchState = await _batchStateService.GetOrCreateBatchStateAsync(batchDate);
                
                _logger.LogInformation(
                    "Batch state initialized. BatchId: {BatchId}, Status: {Status}, RegistrationPhase: {Phase}",
                    batchState.Id, batchState.Status, batchState.RegistrationPhase);

                // Check if job can proceed (check for manual intervention, delays, etc.)
                var (canProceed, reason) = await _jobSchedulingService.CanJobProceedAsync(
                    batchState.Id, 
                    BatchPhase.Registration);
                
                if (!canProceed)
                {
                    _logger.LogWarning(
                        "Job cannot proceed for batch {BatchId}. Reason: {Reason}",
                        batchState.Id, reason);
                    return;
                }

                // If registration is already completed, skip
                if (batchState.RegistrationPhase == PhaseStatus.Completed)
                {
                    _logger.LogInformation(
                        "Registration phase already completed for batch {BatchId}. Skipping.",
                        batchState.Id);
                    return;
                }

                // Update status to running
                await _batchStateService.UpdateBatchStatusAsync(batchState.Id, BatchStatus.Running);
                await _batchStateService.UpdatePhaseStatusAsync(
                    batchState.Id, 
                    BatchPhase.Registration, 
                    PhaseStatus.Running);

                // Get total page count from Rakuten API
                int totalPages;
                try
                {
                    totalPages = await _comicService.GetPageCountAsync();
                    _logger.LogInformation("Total pages to process: {TotalPages}", totalPages);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to get page count from Rakuten API for batch {BatchId}", batchState.Id);
                    
                    // Handle failure with retry logic
                    var willRetry = await _jobSchedulingService.HandleJobFailureAsync(
                        batchState.Id, 
                        BatchPhase.Registration, 
                        ex);
                    
                    if (!willRetry)
                    {
                        await _batchStateService.UpdatePhaseStatusAsync(
                            batchState.Id, 
                            BatchPhase.Registration, 
                            PhaseStatus.Failed);
                    }
                    
                    return;
                }

                // Determine starting page (resume from checkpoint if exists)
                int startPage = batchState.ProcessedPages + 1;
                int successfulPages = batchState.ProcessedPages;
                int failedPages = batchState.FailedPages;

                _logger.LogInformation(
                    "Starting page processing. Start page: {StartPage}, Total pages: {TotalPages}",
                    startPage, totalPages);

                // Process pages sequentially with rate limiting
                for (int currentPage = startPage; currentPage <= totalPages; currentPage++)
                {
                    if (stoppingToken.IsCancellationRequested)
                    {
                        _logger.LogWarning(
                            "Cancellation requested at page {CurrentPage}. Saving checkpoint.",
                            currentPage);
                        break;
                    }

                    try
                    {
                        _logger.LogInformation(
                            "Processing page {CurrentPage}/{TotalPages} for batch {BatchId}",
                            currentPage, totalPages, batchState.Id);

                        // Register comics from current page
                        await _comicService.RegitoryAsync(currentPage);
                        
                        successfulPages++;
                        
                        // Update checkpoint after successful page
                        await _batchStateService.UpdateProgressAsync(
                            batchState.Id, 
                            successfulPages, 
                            failedPages);
                        
                        _logger.LogInformation(
                            "Successfully processed page {CurrentPage}/{TotalPages}. Progress: {SuccessfulPages} successful, {FailedPages} failed",
                            currentPage, totalPages, successfulPages, failedPages);

                        // Rate limiting: Wait 120 seconds before next API call (except for last page)
                        if (currentPage < totalPages)
                        {
                            _logger.LogDebug(
                                "Rate limiting: waiting {DelaySeconds} seconds before next API call",
                                RateLimitDelaySeconds);
                            
                            await Task.Delay(TimeSpan.FromSeconds(RateLimitDelaySeconds), stoppingToken);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex, 
                            "Failed to process page {CurrentPage}/{TotalPages} for batch {BatchId}",
                            currentPage, totalPages, batchState.Id);

                        failedPages++;
                        
                        // Record page-level error
                        await _batchStateService.RecordPageErrorAsync(
                            batchState.Id,
                            currentPage,
                            BatchPhase.Registration,
                            ex.GetType().Name,
                            ex.Message);
                        
                        // Update progress with failed page count
                        await _batchStateService.UpdateProgressAsync(
                            batchState.Id, 
                            successfulPages, 
                            failedPages);

                        // For page-level errors, we continue to next page rather than stopping the entire job
                        // The error is recorded and can be retried later using PartialRetryService
                        _logger.LogInformation(
                            "Continuing to next page after error. Progress: {SuccessfulPages} successful, {FailedPages} failed",
                            successfulPages, failedPages);
                    }
                }

                // Check if all pages were processed successfully
                bool allPagesProcessed = successfulPages >= totalPages;
                bool hasFailures = failedPages > 0;

                if (allPagesProcessed && !hasFailures)
                {
                    // Complete success
                    await _batchStateService.UpdatePhaseStatusAsync(
                        batchState.Id, 
                        BatchPhase.Registration, 
                        PhaseStatus.Completed);
                    
                    await _batchStateService.UpdateBatchStatusAsync(
                        batchState.Id, 
                        BatchStatus.Completed);
                    
                    // Reset retry counter on success
                    await _jobSchedulingService.ResetRetryCounterAsync(batchState.Id);
                    
                    _logger.LogInformation(
                        "Registration phase completed successfully for batch {BatchId}. Processed {TotalPages} pages.",
                        batchState.Id, totalPages);
                }
                else if (hasFailures)
                {
                    // Partial success - some pages failed
                    await _batchStateService.UpdatePhaseStatusAsync(
                        batchState.Id, 
                        BatchPhase.Registration, 
                        PhaseStatus.Completed);
                    
                    _logger.LogWarning(
                        "Registration phase completed with failures for batch {BatchId}. Success: {SuccessfulPages}, Failed: {FailedPages}",
                        batchState.Id, successfulPages, failedPages);
                }
                else
                {
                    // Job was interrupted or cancelled
                    _logger.LogInformation(
                        "Registration phase interrupted for batch {BatchId}. Progress saved at page {ProcessedPages}/{TotalPages}",
                        batchState.Id, successfulPages, totalPages);
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical(
                    ex, 
                    "Unhandled exception in Data Registration Job. Job will terminate.");
                
                // This is a job-level failure, not a page-level failure
                // Exception is logged, and the application will terminate gracefully via finally block
            }
            finally
            {
                // Signal application shutdown
                _applicationLifetime.StopApplication();
            }
        }
    }
}
