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
    /// Container Job for comic image downloads from Rakuten Books API to Blob Storage
    /// Implements checkpoint-based processing with 30-second rate limiting and dependency checking
    /// </summary>
    public class ImageDownloadJob : BackgroundService
    {
        private readonly IComicService _comicService;
        private readonly IBatchStateService _batchStateService;
        private readonly JobSchedulingService _jobSchedulingService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ImageDownloadJob> _logger;
        private readonly IHostApplicationLifetime _applicationLifetime;
        
        // Rate limiting: 30 seconds between API calls as per Rakuten API requirements
        private const int RateLimitDelaySeconds = 30;
        
        // Job type identifier from environment variable
        private const string JobTypeEnvironmentVariable = "BATCH_JOB_TYPE";
        private const string ExpectedJobType = "ImageDownload";

        public ImageDownloadJob(
            IComicService comicService,
            IBatchStateService batchStateService,
            JobSchedulingService jobSchedulingService,
            IConfiguration configuration,
            ILogger<ImageDownloadJob> logger,
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
            // Check if this job should run based on environment variable
            var jobType = _configuration[JobTypeEnvironmentVariable];
            if (string.IsNullOrWhiteSpace(jobType) || !jobType.Equals(ExpectedJobType, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation(
                    "Job type mismatch. Expected: {ExpectedJobType}, Actual: {ActualJobType}. Skipping execution.",
                    ExpectedJobType, jobType ?? "null");
                return;
            }

            try
            {

                _logger.LogInformation("Starting Image Download Job for {ExpectedJobType}", ExpectedJobType);

                // Get or create batch state for today
                var batchDate = DateTime.UtcNow.Date;
                var batchState = await _batchStateService.GetOrCreateBatchStateAsync(batchDate);
                
                _logger.LogInformation(
                    "Batch state initialized. BatchId: {BatchId}, Status: {Status}, ImageDownloadPhase: {Phase}",
                    batchState.Id, batchState.Status, batchState.ImageDownloadPhase);

                // Check if job can proceed (check for manual intervention, delays, dependencies)
                var (canProceed, reason) = await _jobSchedulingService.CanJobProceedAsync(
                    batchState.Id, 
                    BatchPhase.ImageDownload);
                
                if (!canProceed)
                {
                    _logger.LogWarning(
                        "Job cannot proceed for batch {BatchId}. Reason: {Reason}",
                        batchState.Id, reason);
                    return;
                }

                // If image download is already completed, skip
                if (batchState.ImageDownloadPhase == PhaseStatus.Completed)
                {
                    _logger.LogInformation(
                        "Image download phase already completed for batch {BatchId}. Skipping.",
                        batchState.Id);
                    return;
                }

                // Update status to running
                await _batchStateService.UpdateBatchStatusAsync(batchState.Id, BatchStatus.Running);
                await _batchStateService.UpdatePhaseStatusAsync(
                    batchState.Id, 
                    BatchPhase.ImageDownload, 
                    PhaseStatus.Running);

                // Get total page count from Rakuten API
                int totalPages;
                try
                {
                    totalPages = await _comicService.GetPageCountAsync();
                    _logger.LogInformation("Total pages to process for image download: {TotalPages}", totalPages);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to get page count from Rakuten API for batch {BatchId}", batchState.Id);
                    
                    // Handle failure with retry logic
                    var willRetry = await _jobSchedulingService.HandleJobFailureAsync(
                        batchState.Id, 
                        BatchPhase.ImageDownload, 
                        ex);
                    
                    if (!willRetry)
                    {
                        await _batchStateService.UpdatePhaseStatusAsync(
                            batchState.Id, 
                            BatchPhase.ImageDownload, 
                            PhaseStatus.Failed);
                    }
                    
                    return;
                }

                // Process all pages for image download
                // Each page is processed independently, and existing images are automatically skipped
                int startPage = 1;
                int successfulPages = 0;
                int failedPages = 0;

                _logger.LogInformation(
                    "Starting image download. Total pages: {TotalPages}",
                    totalPages);

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
                            "Processing images for page {CurrentPage}/{TotalPages} for batch {BatchId}",
                            currentPage, totalPages, batchState.Id);

                        // Download and save images from current page
                        await _comicService.ProcessImageDownloadsAsync(currentPage);
                        
                        successfulPages++;
                        
                        _logger.LogInformation(
                            "Successfully processed images for page {CurrentPage}/{TotalPages}. Progress: {SuccessfulPages} successful, {FailedPages} failed",
                            currentPage, totalPages, successfulPages, failedPages);

                        // Rate limiting: Wait 30 seconds before next API call (except for last page)
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
                            "Failed to process images for page {CurrentPage}/{TotalPages} for batch {BatchId}",
                            currentPage, totalPages, batchState.Id);

                        failedPages++;
                        
                        // Record page-level error
                        await _batchStateService.RecordPageErrorAsync(
                            batchState.Id,
                            currentPage,
                            BatchPhase.ImageDownload,
                            ex.GetType().Name,
                            ex.Message);

                        // For page-level errors, we continue to next page rather than stopping the entire job
                        // The error is recorded and can be retried later using PartialRetryService
                        _logger.LogInformation(
                            "Continuing to next page after error. Progress: {SuccessfulPages} successful, {FailedPages} failed",
                            successfulPages, failedPages);
                    }
                }

                // Check if all pages were processed successfully
                bool allPagesProcessed = successfulPages == totalPages;
                bool hasFailures = failedPages > 0;

                if (allPagesProcessed && !hasFailures)
                {
                    // Complete success
                    await _batchStateService.UpdatePhaseStatusAsync(
                        batchState.Id, 
                        BatchPhase.ImageDownload, 
                        PhaseStatus.Completed);
                    
                    await _batchStateService.UpdateBatchStatusAsync(
                        batchState.Id, 
                        BatchStatus.Completed);
                    
                    // Reset retry counter on success
                    await _jobSchedulingService.ResetRetryCounterAsync(batchState.Id);
                    
                    _logger.LogInformation(
                        "Image download phase completed successfully for batch {BatchId}. Processed {TotalPages} pages.",
                        batchState.Id, totalPages);
                }
                else if (hasFailures)
                {
                    // Partial success - some pages failed
                    await _batchStateService.UpdatePhaseStatusAsync(
                        batchState.Id, 
                        BatchPhase.ImageDownload, 
                        PhaseStatus.Completed);
                    
                    _logger.LogWarning(
                        "Image download phase completed with failures for batch {BatchId}. Success: {SuccessfulPages}, Failed: {FailedPages}",
                        batchState.Id, successfulPages, failedPages);
                }
                else
                {
                    // Job was interrupted or cancelled
                    _logger.LogInformation(
                        "Image download phase interrupted for batch {BatchId}. Progress saved at page {ProcessedPages}/{TotalPages}",
                        batchState.Id, successfulPages, totalPages);
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical(
                    ex, 
                    "Unhandled exception in Image Download Job. Job will terminate.");
                
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
