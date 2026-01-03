using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ComiCal.Shared.Models;

namespace ComiCal.Batch.Repositories
{
    /// <summary>
    /// Repository interface for batch state operations
    /// </summary>
    public interface IBatchStateRepository
    {
        Task<BatchState> GetOrCreateAsync(DateTime batchDate);
        Task<BatchState?> GetByIdAsync(int batchId);
        Task<BatchState?> GetByDateAsync(DateTime batchDate);
        Task UpdateStatusAsync(int batchId, string status, string? errorMessage = null);
        Task UpdatePhaseAsync(int batchId, string phase, string status);
        Task UpdateProgressAsync(int batchId, int processedPages, int failedPages);
        Task SetDelayAsync(int batchId, DateTime delayedUntil, int retryAttempts);
        Task SetManualInterventionAsync(int batchId, bool required, string? errorMessage = null);
        Task<IEnumerable<BatchState>> GetReadyToResumeAsync();
        Task<IEnumerable<BatchPageError>> GetUnresolvedErrorsAsync(int batchId);
        Task RecordPageErrorAsync(BatchPageError error);
        Task MarkErrorsAsResolvedAsync(int batchId, IEnumerable<int> pageNumbers, string phase);
        Task DeletePageErrorsAsync(int batchId, IEnumerable<int> pageNumbers, string phase);
    }
}
