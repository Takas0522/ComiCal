using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ComiCal.Shared.Models;

namespace ComiCal.Batch.Services
{
    /// <summary>
    /// Service interface for managing batch execution state
    /// </summary>
    public interface IBatchStateService
    {
        /// <summary>
        /// Get or create a batch state for a specific date
        /// </summary>
        Task<BatchState> GetOrCreateBatchStateAsync(DateTime batchDate);

        /// <summary>
        /// Update the status of a batch
        /// </summary>
        Task UpdateBatchStatusAsync(int batchId, string status, string? errorMessage = null);

        /// <summary>
        /// Update phase status for registration or image download
        /// </summary>
        Task UpdatePhaseStatusAsync(int batchId, string phase, string status);

        /// <summary>
        /// Update page processing progress
        /// </summary>
        Task UpdateProgressAsync(int batchId, int processedPages, int failedPages);

        /// <summary>
        /// Set delay for a batch execution
        /// </summary>
        Task SetDelayAsync(int batchId, DateTime delayedUntil, int retryAttempts);

        /// <summary>
        /// Set manual intervention flag
        /// </summary>
        Task SetManualInterventionAsync(int batchId, bool required, string? errorMessage = null);

        /// <summary>
        /// Clear manual intervention and prepare for auto-resume
        /// </summary>
        Task ClearManualInterventionAsync(int batchId);

        /// <summary>
        /// Get batch state by ID
        /// </summary>
        Task<BatchState?> GetBatchStateAsync(int batchId);

        /// <summary>
        /// Get batch state by date
        /// </summary>
        Task<BatchState?> GetBatchStateByDateAsync(DateTime batchDate);

        /// <summary>
        /// Get batches that are ready to resume (delayed_until has passed)
        /// </summary>
        Task<IEnumerable<BatchState>> GetBatchesReadyToResumeAsync();

        /// <summary>
        /// Record a page error
        /// </summary>
        Task RecordPageErrorAsync(int batchId, int pageNumber, string phase, string errorType, string errorMessage);

        /// <summary>
        /// Get all unresolved errors for a batch
        /// </summary>
        Task<IEnumerable<BatchPageError>> GetUnresolvedErrorsAsync(int batchId);

        /// <summary>
        /// Mark page errors as resolved
        /// </summary>
        Task MarkErrorsAsResolvedAsync(int batchId, IEnumerable<int> pageNumbers, string phase);

        /// <summary>
        /// Reset page state for partial retry (clears errors and resets progress)
        /// </summary>
        Task ResetPagesForRetryAsync(int batchId, IEnumerable<int> pageNumbers, string phase);
    }
}
