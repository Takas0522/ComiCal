using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ComiCal.Batch.Repositories;
using ComiCal.Shared.Models;
using Microsoft.Extensions.Logging;

namespace ComiCal.Batch.Services
{
    /// <summary>
    /// Service implementation for batch state management
    /// </summary>
    public class BatchStateService : IBatchStateService
    {
        private readonly IBatchStateRepository _repository;
        private readonly ILogger<BatchStateService> _logger;

        public BatchStateService(IBatchStateRepository repository, ILogger<BatchStateService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<BatchState> GetOrCreateBatchStateAsync(DateTime batchDate)
        {
            return await _repository.GetOrCreateAsync(batchDate.Date);
        }

        public async Task UpdateBatchStatusAsync(int batchId, string status, string? errorMessage = null)
        {
            await _repository.UpdateStatusAsync(batchId, status, errorMessage);
            _logger.LogInformation("Updated batch {BatchId} status to {Status}", batchId, status);
        }

        public async Task UpdatePhaseStatusAsync(int batchId, string phase, string status)
        {
            await _repository.UpdatePhaseAsync(batchId, phase, status);
            _logger.LogDebug("Updated batch {BatchId} {Phase} phase to {Status}", batchId, phase, status);
        }

        public async Task UpdateProgressAsync(int batchId, int processedPages, int failedPages)
        {
            await _repository.UpdateProgressAsync(batchId, processedPages, failedPages);
        }

        public async Task SetDelayAsync(int batchId, DateTime delayedUntil, int retryAttempts)
        {
            await _repository.SetDelayAsync(batchId, delayedUntil, retryAttempts);
            _logger.LogInformation(
                "Set batch {BatchId} delay until {DelayedUntil}, retry attempt {RetryAttempts}",
                batchId, delayedUntil, retryAttempts);
        }

        public async Task SetManualInterventionAsync(int batchId, bool required, string? errorMessage = null)
        {
            await _repository.SetManualInterventionAsync(batchId, required, errorMessage);
            _logger.LogWarning("Set manual intervention for batch {BatchId}: {Required}", batchId, required);
        }

        public async Task ClearManualInterventionAsync(int batchId)
        {
            await _repository.SetManualInterventionAsync(batchId, false);
            
            // Reset status to pending if currently in manual intervention
            var batchState = await _repository.GetByIdAsync(batchId);
            if (batchState?.Status == BatchStatus.ManualIntervention)
            {
                await _repository.UpdateStatusAsync(batchId, BatchStatus.Pending);
            }
            
            _logger.LogInformation("Cleared manual intervention for batch {BatchId}", batchId);
        }

        public async Task<BatchState?> GetBatchStateAsync(int batchId)
        {
            return await _repository.GetByIdAsync(batchId);
        }

        public async Task<BatchState?> GetBatchStateByDateAsync(DateTime batchDate)
        {
            return await _repository.GetByDateAsync(batchDate.Date);
        }

        public async Task<IEnumerable<BatchState>> GetBatchesReadyToResumeAsync()
        {
            return await _repository.GetReadyToResumeAsync();
        }

        public async Task RecordPageErrorAsync(int batchId, int pageNumber, string phase, string errorType, string errorMessage)
        {
            var error = new BatchPageError
            {
                BatchId = batchId,
                PageNumber = pageNumber,
                Phase = phase,
                ErrorType = errorType,
                ErrorMessage = errorMessage,
                RetryCount = 0,
                LastRetryAt = DateTime.UtcNow,
                Resolved = false
            };

            await _repository.RecordPageErrorAsync(error);
            _logger.LogWarning(
                "Recorded error for batch {BatchId}, page {PageNumber}, phase {Phase}: {ErrorType}",
                batchId, pageNumber, phase, errorType);
        }

        public async Task<IEnumerable<BatchPageError>> GetUnresolvedErrorsAsync(int batchId)
        {
            return await _repository.GetUnresolvedErrorsAsync(batchId);
        }

        public async Task MarkErrorsAsResolvedAsync(int batchId, IEnumerable<int> pageNumbers, string phase)
        {
            await _repository.MarkErrorsAsResolvedAsync(batchId, pageNumbers, phase);
        }

        public async Task ResetPagesForRetryAsync(int batchId, IEnumerable<int> pageNumbers, string phase)
        {
            // Delete errors to allow retry
            await _repository.DeletePageErrorsAsync(batchId, pageNumbers, phase);
            _logger.LogInformation("Reset pages for retry in batch {BatchId}, phase {Phase}", batchId, phase);
        }
    }
}
