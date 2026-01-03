using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ComiCal.Batch.Repositories;
using ComiCal.Shared.Models;
using Microsoft.Extensions.Logging;

namespace ComiCal.Batch.Services
{
    /// <summary>
    /// Service for managing partial retries and checkpoint-based recovery
    /// </summary>
    public class PartialRetryService
    {
        private readonly IBatchStateRepository _batchStateRepository;
        private readonly ILogger<PartialRetryService> _logger;

        public PartialRetryService(
            IBatchStateRepository batchStateRepository,
            ILogger<PartialRetryService> logger)
        {
            _batchStateRepository = batchStateRepository;
            _logger = logger;
        }

        /// <summary>
        /// Reset specific page range for retry (clears errors and resets progress)
        /// </summary>
        public async Task ResetPageRangeAsync(int batchId, int startPage, int endPage, string phase)
        {
            if (startPage < 1 || endPage < startPage)
            {
                throw new ArgumentException($"Invalid page range: {startPage}-{endPage}");
            }

            var batchState = await _batchStateRepository.GetByIdAsync(batchId);
            if (batchState == null)
            {
                throw new InvalidOperationException($"Batch state not found for ID {batchId}");
            }

            var pageNumbers = Enumerable.Range(startPage, endPage - startPage + 1).ToList();
            
            // Delete existing errors for these pages
            await _batchStateRepository.DeletePageErrorsAsync(batchId, pageNumbers, phase);
            
            _logger.LogInformation(
                "Reset page range {StartPage}-{EndPage} for batch {BatchId}, phase {Phase}",
                startPage, endPage, batchId, phase);
        }

        /// <summary>
        /// Get all pages that have errors and need retry
        /// </summary>
        public async Task<IEnumerable<int>> GetErrorPagesAsync(int batchId, string phase)
        {
            var errors = await _batchStateRepository.GetUnresolvedErrorsAsync(batchId);
            var errorPages = errors
                .Where(e => e.Phase == phase)
                .Select(e => e.PageNumber)
                .Distinct()
                .OrderBy(p => p)
                .ToList();

            if (errorPages.Any())
            {
                _logger.LogInformation(
                    "Found {Count} error pages for batch {BatchId}, phase {Phase}: {Pages}",
                    errorPages.Count, batchId, phase, string.Join(", ", errorPages));
            }

            return errorPages;
        }

        /// <summary>
        /// Reset only error pages for retry
        /// </summary>
        public async Task ResetErrorPagesAsync(int batchId, string phase)
        {
            var errorPages = await GetErrorPagesAsync(batchId, phase);
            var pageList = errorPages.ToList();

            if (!pageList.Any())
            {
                _logger.LogInformation("No error pages to reset for batch {BatchId}, phase {Phase}", batchId, phase);
                return;
            }

            // Delete errors for these pages to allow retry
            await _batchStateRepository.DeletePageErrorsAsync(batchId, pageList, phase);
            
            // Update failed pages count
            var batchState = await _batchStateRepository.GetByIdAsync(batchId);
            if (batchState != null)
            {
                var newFailedCount = Math.Max(0, batchState.FailedPages - pageList.Count);
                await _batchStateRepository.UpdateProgressAsync(
                    batchId, 
                    batchState.ProcessedPages, 
                    newFailedCount);
            }

            _logger.LogInformation(
                "Reset {Count} error pages for batch {BatchId}, phase {Phase}",
                pageList.Count, batchId, phase);
        }

        /// <summary>
        /// Mark a checkpoint - record current progress for recovery
        /// </summary>
        public async Task MarkCheckpointAsync(int batchId, int processedPages, int failedPages)
        {
            await _batchStateRepository.UpdateProgressAsync(batchId, processedPages, failedPages);
            
            _logger.LogDebug(
                "Checkpoint marked for batch {BatchId}: Processed={Processed}, Failed={Failed}",
                batchId, processedPages, failedPages);
        }

        /// <summary>
        /// Get the current progress checkpoint
        /// </summary>
        public async Task<(int ProcessedPages, int FailedPages)?> GetCheckpointAsync(int batchId)
        {
            var batchState = await _batchStateRepository.GetByIdAsync(batchId);
            if (batchState == null)
            {
                return null;
            }

            return (batchState.ProcessedPages, batchState.FailedPages);
        }

        /// <summary>
        /// Get unresolved errors with details for a batch
        /// </summary>
        public async Task<IEnumerable<BatchPageError>> GetUnresolvedErrorDetailsAsync(int batchId, string? phase = null)
        {
            var errors = await _batchStateRepository.GetUnresolvedErrorsAsync(batchId);
            
            if (!string.IsNullOrEmpty(phase))
            {
                errors = errors.Where(e => e.Phase == phase);
            }

            var errorList = errors.ToList();
            
            if (errorList.Any())
            {
                if (phase != null)
                {
                    _logger.LogInformation(
                        "Found {Count} unresolved errors for batch {BatchId}, phase {Phase}",
                        errorList.Count, batchId, phase);
                }
                else
                {
                    _logger.LogInformation(
                        "Found {Count} unresolved errors for batch {BatchId}",
                        errorList.Count, batchId);
                }
            }

            return errorList;
        }

        /// <summary>
        /// Mark pages as successfully processed (resolves errors)
        /// </summary>
        public async Task MarkPagesAsSuccessfulAsync(int batchId, IEnumerable<int> pageNumbers, string phase)
        {
            var pageList = pageNumbers.ToList();
            if (!pageList.Any())
            {
                return;
            }

            await _batchStateRepository.MarkErrorsAsResolvedAsync(batchId, pageList, phase);
            
            _logger.LogInformation(
                "Marked {Count} pages as successful for batch {BatchId}, phase {Phase}",
                pageList.Count, batchId, phase);
        }

        /// <summary>
        /// Calculate retry statistics for a batch
        /// </summary>
        public async Task<RetryStatistics> GetRetryStatisticsAsync(int batchId)
        {
            var batchState = await _batchStateRepository.GetByIdAsync(batchId);
            if (batchState == null)
            {
                throw new InvalidOperationException($"Batch state not found for ID {batchId}");
            }

            var errors = await _batchStateRepository.GetUnresolvedErrorsAsync(batchId);
            var errorList = errors.ToList();

            var registrationErrors = errorList.Count(e => e.Phase == BatchPhase.Registration);
            var imageDownloadErrors = errorList.Count(e => e.Phase == BatchPhase.ImageDownload);

            return new RetryStatistics
            {
                BatchId = batchId,
                TotalPages = batchState.TotalPages ?? 0,
                ProcessedPages = batchState.ProcessedPages,
                FailedPages = batchState.FailedPages,
                RegistrationErrors = registrationErrors,
                ImageDownloadErrors = imageDownloadErrors,
                RetryAttempts = batchState.RetryAttempts,
                CanRetry = !batchState.ManualInterventionRequired
            };
        }

        /// <summary>
        /// Prepare batch for full retry (reset all progress)
        /// </summary>
        public async Task ResetBatchForFullRetryAsync(int batchId)
        {
            var batchState = await _batchStateRepository.GetByIdAsync(batchId);
            if (batchState == null)
            {
                throw new InvalidOperationException($"Batch state not found for ID {batchId}");
            }

            // Reset progress
            await _batchStateRepository.UpdateProgressAsync(batchId, 0, 0);

            // Reset phases to pending
            await _batchStateRepository.UpdatePhaseAsync(batchId, BatchPhase.Registration, PhaseStatus.Pending);
            await _batchStateRepository.UpdatePhaseAsync(batchId, BatchPhase.ImageDownload, PhaseStatus.Pending);

            // Clear all errors (they will be re-recorded on retry)
            var errors = await _batchStateRepository.GetUnresolvedErrorsAsync(batchId);
            var allPages = errors.Select(e => e.PageNumber).Distinct().ToList();
            
            if (allPages.Any())
            {
                await _batchStateRepository.DeletePageErrorsAsync(batchId, allPages, BatchPhase.Registration);
                await _batchStateRepository.DeletePageErrorsAsync(batchId, allPages, BatchPhase.ImageDownload);
            }

            // Reset status
            await _batchStateRepository.UpdateStatusAsync(batchId, BatchStatus.Pending);

            _logger.LogInformation("Reset batch {BatchId} for full retry", batchId);
        }
    }

    /// <summary>
    /// Statistics for retry operations
    /// </summary>
    public class RetryStatistics
    {
        public int BatchId { get; set; }
        public int TotalPages { get; set; }
        public int ProcessedPages { get; set; }
        public int FailedPages { get; set; }
        public int RegistrationErrors { get; set; }
        public int ImageDownloadErrors { get; set; }
        public int RetryAttempts { get; set; }
        public bool CanRetry { get; set; }
    }
}
