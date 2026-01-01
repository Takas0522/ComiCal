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
    /// Service for managing job scheduling, delays, dependencies, and manual intervention
    /// </summary>
    public class JobSchedulingService
    {
        private readonly IBatchStateRepository _batchStateRepository;
        private readonly ILogger<JobSchedulingService> _logger;
        private const int MaxRetryAttempts = 3;
        
        // Delay intervals for retry attempts as per business requirements
        // First retry: 5 minutes, Second: 15 minutes, Third: 30 minutes
        // These values are part of the business logic and should remain consistent
        // across environments unless requirements change
        private static readonly TimeSpan[] DelayIntervals = new[]
        {
            TimeSpan.FromMinutes(5),   // First retry: 5 minutes
            TimeSpan.FromMinutes(15),  // Second retry: 15 minutes
            TimeSpan.FromMinutes(30)   // Third retry: 30 minutes
        };

        public JobSchedulingService(
            IBatchStateRepository batchStateRepository,
            ILogger<JobSchedulingService> logger)
        {
            _batchStateRepository = batchStateRepository;
            _logger = logger;
        }

        /// <summary>
        /// Check if a job can proceed based on dependencies and current state
        /// </summary>
        public async Task<(bool CanProceed, string? Reason)> CanJobProceedAsync(int batchId, string phase)
        {
            var batchState = await _batchStateRepository.GetByIdAsync(batchId);
            if (batchState == null)
            {
                return (false, "Batch state not found");
            }

            // Check if manual intervention is required
            if (batchState.ManualInterventionRequired)
            {
                return (false, "Manual intervention required - batch is paused");
            }

            // Check if batch is delayed
            if (batchState.Status == BatchStatus.Delayed && batchState.DelayedUntil.HasValue)
            {
                if (batchState.DelayedUntil.Value > DateTime.UtcNow)
                {
                    return (false, $"Batch is delayed until {batchState.DelayedUntil.Value:yyyy-MM-dd HH:mm:ss} UTC");
                }
            }

            // Check phase dependencies: image download requires registration to be completed
            if (phase == BatchPhase.ImageDownload)
            {
                if (batchState.RegistrationPhase != PhaseStatus.Completed)
                {
                    return (false, "Registration phase must be completed before image download can proceed");
                }
            }

            return (true, null);
        }

        /// <summary>
        /// Handle job failure with automatic retry and delay logic
        /// </summary>
        public async Task<bool> HandleJobFailureAsync(int batchId, string phase, Exception exception)
        {
            var batchState = await _batchStateRepository.GetByIdAsync(batchId);
            if (batchState == null)
            {
                _logger.LogError("Batch state not found for ID {BatchId}", batchId);
                return false;
            }

            var currentRetry = batchState.RetryAttempts;
            _logger.LogWarning(exception, 
                "Job failure for batch {BatchId}, phase {Phase}. Retry attempt: {RetryAttempt}/{MaxRetries}",
                batchId, phase, currentRetry, MaxRetryAttempts);

            // Check if we've exceeded max retries
            if (currentRetry >= MaxRetryAttempts)
            {
                _logger.LogError(
                    "Max retry attempts ({MaxRetries}) reached for batch {BatchId}, phase {Phase}. Requiring manual intervention.",
                    MaxRetryAttempts, batchId, phase);

                await _batchStateRepository.SetManualInterventionAsync(
                    batchId, 
                    true, 
                    $"Max retry attempts reached after {MaxRetryAttempts} failures. Last error: {exception.Message}");

                await _batchStateRepository.UpdatePhaseAsync(batchId, phase, PhaseStatus.Failed);
                
                return false; // Cannot auto-retry
            }

            // Schedule retry with exponential backoff
            var delayInterval = DelayIntervals[currentRetry];
            var delayedUntil = DateTime.UtcNow.Add(delayInterval);
            
            await _batchStateRepository.SetDelayAsync(batchId, delayedUntil, currentRetry + 1);
            
            _logger.LogInformation(
                "Scheduled retry {RetryAttempt}/{MaxRetries} for batch {BatchId} at {DelayedUntil}. Delay interval: {Interval}",
                currentRetry + 1, MaxRetryAttempts, batchId, delayedUntil, delayInterval);

            return true; // Will auto-retry
        }

        /// <summary>
        /// Get batches that are ready to resume after delay period
        /// </summary>
        public async Task<IEnumerable<BatchState>> GetBatchesReadyToResumeAsync()
        {
            var batches = await _batchStateRepository.GetReadyToResumeAsync();
            var batchList = batches.ToList();
            
            if (batchList.Any())
            {
                _logger.LogInformation("Found {Count} batches ready to resume", batchList.Count);
            }
            
            return batchList;
        }

        /// <summary>
        /// Clear manual intervention flag and enable auto-resume
        /// </summary>
        public async Task ClearManualInterventionAsync(int batchId)
        {
            var batchState = await _batchStateRepository.GetByIdAsync(batchId);
            if (batchState == null)
            {
                throw new InvalidOperationException($"Batch state not found for ID {batchId}");
            }

            if (!batchState.ManualInterventionRequired)
            {
                _logger.LogInformation("Manual intervention already cleared for batch {BatchId}", batchId);
                return;
            }

            await _batchStateRepository.SetManualInterventionAsync(batchId, false);
            
            // Reset status from manual_intervention to pending if it was in that state
            if (batchState.Status == BatchStatus.ManualIntervention)
            {
                await _batchStateRepository.UpdateStatusAsync(batchId, BatchStatus.Pending);
            }

            // Reset retry counter to allow fresh retry attempts
            await _batchStateRepository.SetDelayAsync(batchId, DateTime.UtcNow, 0);
            
            _logger.LogInformation("Cleared manual intervention for batch {BatchId} - ready for auto-resume", batchId);
        }

        /// <summary>
        /// Set manual intervention flag (for external API calls)
        /// </summary>
        public async Task SetManualInterventionAsync(int batchId, string reason)
        {
            await _batchStateRepository.SetManualInterventionAsync(batchId, true, reason);
            _logger.LogWarning("Manual intervention set for batch {BatchId}: {Reason}", batchId, reason);
        }

        /// <summary>
        /// Reset retry counter (useful after successful recovery)
        /// </summary>
        public async Task ResetRetryCounterAsync(int batchId)
        {
            await _batchStateRepository.SetDelayAsync(batchId, DateTime.UtcNow, 0);
            _logger.LogInformation("Reset retry counter for batch {BatchId}", batchId);
        }

        /// <summary>
        /// Check if job has dependency on another phase completion
        /// </summary>
        public async Task<bool> CheckPhaseDependencyAsync(int batchId, string phase)
        {
            if (phase != BatchPhase.ImageDownload)
            {
                return true; // No dependencies for registration phase
            }

            var batchState = await _batchStateRepository.GetByIdAsync(batchId);
            if (batchState == null)
            {
                _logger.LogError("Batch state not found for ID {BatchId}", batchId);
                return false;
            }

            var isCompleted = batchState.RegistrationPhase == PhaseStatus.Completed;
            
            if (!isCompleted)
            {
                _logger.LogWarning(
                    "Phase dependency check failed for batch {BatchId}: Image download requires registration phase to be completed (current: {Status})",
                    batchId, batchState.RegistrationPhase);
            }
            
            return isCompleted;
        }
    }
}
