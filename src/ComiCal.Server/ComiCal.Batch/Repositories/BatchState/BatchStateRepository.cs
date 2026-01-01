using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ComiCal.Shared.Models;
using Dapper;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace ComiCal.Batch.Repositories
{
    /// <summary>
    /// Repository for batch state and error tracking operations
    /// </summary>
    public class BatchStateRepository : IBatchStateRepository
    {
        private readonly NpgsqlDataSource _dataSource;
        private readonly ILogger<BatchStateRepository> _logger;

        public BatchStateRepository(NpgsqlDataSource dataSource, ILogger<BatchStateRepository> logger)
        {
            _dataSource = dataSource;
            _logger = logger;
        }

        public async Task<BatchState> GetOrCreateAsync(DateTime batchDate)
        {
            const string selectSql = @"
                SELECT 
                    id as Id,
                    batch_date as BatchDate,
                    status as Status,
                    total_pages as TotalPages,
                    processed_pages as ProcessedPages,
                    failed_pages as FailedPages,
                    registration_phase as RegistrationPhase,
                    image_download_phase as ImageDownloadPhase,
                    delayed_until as DelayedUntil,
                    retry_attempts as RetryAttempts,
                    manual_intervention_required as ManualInterventionRequired,
                    auto_resume_enabled as AutoResumeEnabled,
                    error_message as ErrorMessage,
                    created_at as CreatedAt,
                    updated_at as UpdatedAt
                FROM batch_states
                WHERE batch_date = @BatchDate";

            const string insertSql = @"
                INSERT INTO batch_states (
                    batch_date, status, processed_pages, failed_pages, 
                    registration_phase, image_download_phase, retry_attempts,
                    manual_intervention_required, auto_resume_enabled,
                    created_at, updated_at
                ) VALUES (
                    @BatchDate, 'pending', 0, 0,
                    'pending', 'pending', 0,
                    false, true,
                    CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
                )
                RETURNING 
                    id as Id,
                    batch_date as BatchDate,
                    status as Status,
                    total_pages as TotalPages,
                    processed_pages as ProcessedPages,
                    failed_pages as FailedPages,
                    registration_phase as RegistrationPhase,
                    image_download_phase as ImageDownloadPhase,
                    delayed_until as DelayedUntil,
                    retry_attempts as RetryAttempts,
                    manual_intervention_required as ManualInterventionRequired,
                    auto_resume_enabled as AutoResumeEnabled,
                    error_message as ErrorMessage,
                    created_at as CreatedAt,
                    updated_at as UpdatedAt";

            await using var connection = await _dataSource.OpenConnectionAsync();
            
            // Try to get existing
            var existing = await connection.QueryFirstOrDefaultAsync<BatchState>(selectSql, new { BatchDate = batchDate.Date });
            if (existing != null)
            {
                return existing;
            }

            // Create new
            var newBatch = await connection.QueryFirstAsync<BatchState>(insertSql, new { BatchDate = batchDate.Date });
            _logger.LogInformation("Created new batch state for date {BatchDate} with ID {BatchId}", batchDate.Date, newBatch.Id);
            return newBatch;
        }

        public async Task<BatchState?> GetByIdAsync(int batchId)
        {
            const string sql = @"
                SELECT 
                    id as Id,
                    batch_date as BatchDate,
                    status as Status,
                    total_pages as TotalPages,
                    processed_pages as ProcessedPages,
                    failed_pages as FailedPages,
                    registration_phase as RegistrationPhase,
                    image_download_phase as ImageDownloadPhase,
                    delayed_until as DelayedUntil,
                    retry_attempts as RetryAttempts,
                    manual_intervention_required as ManualInterventionRequired,
                    auto_resume_enabled as AutoResumeEnabled,
                    error_message as ErrorMessage,
                    created_at as CreatedAt,
                    updated_at as UpdatedAt
                FROM batch_states
                WHERE id = @BatchId";

            await using var connection = await _dataSource.OpenConnectionAsync();
            return await connection.QueryFirstOrDefaultAsync<BatchState>(sql, new { BatchId = batchId });
        }

        public async Task<BatchState?> GetByDateAsync(DateTime batchDate)
        {
            const string sql = @"
                SELECT 
                    id as Id,
                    batch_date as BatchDate,
                    status as Status,
                    total_pages as TotalPages,
                    processed_pages as ProcessedPages,
                    failed_pages as FailedPages,
                    registration_phase as RegistrationPhase,
                    image_download_phase as ImageDownloadPhase,
                    delayed_until as DelayedUntil,
                    retry_attempts as RetryAttempts,
                    manual_intervention_required as ManualInterventionRequired,
                    auto_resume_enabled as AutoResumeEnabled,
                    error_message as ErrorMessage,
                    created_at as CreatedAt,
                    updated_at as UpdatedAt
                FROM batch_states
                WHERE batch_date = @BatchDate";

            await using var connection = await _dataSource.OpenConnectionAsync();
            return await connection.QueryFirstOrDefaultAsync<BatchState>(sql, new { BatchDate = batchDate.Date });
        }

        public async Task UpdateStatusAsync(int batchId, string status, string? errorMessage = null)
        {
            const string sql = @"
                UPDATE batch_states
                SET status = @Status,
                    error_message = @ErrorMessage,
                    updated_at = CURRENT_TIMESTAMP
                WHERE id = @BatchId";

            await using var connection = await _dataSource.OpenConnectionAsync();
            await connection.ExecuteAsync(sql, new { BatchId = batchId, Status = status, ErrorMessage = errorMessage });
            _logger.LogDebug("Updated batch {BatchId} status to {Status}", batchId, status);
        }

        public async Task UpdatePhaseAsync(int batchId, string phase, string status)
        {
            string sql = phase == BatchPhase.Registration
                ? @"UPDATE batch_states 
                    SET registration_phase = @Status, updated_at = CURRENT_TIMESTAMP 
                    WHERE id = @BatchId"
                : @"UPDATE batch_states 
                    SET image_download_phase = @Status, updated_at = CURRENT_TIMESTAMP 
                    WHERE id = @BatchId";

            await using var connection = await _dataSource.OpenConnectionAsync();
            await connection.ExecuteAsync(sql, new { BatchId = batchId, Status = status });
            _logger.LogDebug("Updated batch {BatchId} {Phase} phase to {Status}", batchId, phase, status);
        }

        public async Task UpdateProgressAsync(int batchId, int processedPages, int failedPages)
        {
            const string sql = @"
                UPDATE batch_states
                SET processed_pages = @ProcessedPages,
                    failed_pages = @FailedPages,
                    updated_at = CURRENT_TIMESTAMP
                WHERE id = @BatchId";

            await using var connection = await _dataSource.OpenConnectionAsync();
            await connection.ExecuteAsync(sql, new { BatchId = batchId, ProcessedPages = processedPages, FailedPages = failedPages });
        }

        public async Task SetDelayAsync(int batchId, DateTime delayedUntil, int retryAttempts)
        {
            const string sql = @"
                UPDATE batch_states
                SET status = @Status,
                    delayed_until = @DelayedUntil,
                    retry_attempts = @RetryAttempts,
                    updated_at = CURRENT_TIMESTAMP
                WHERE id = @BatchId";

            await using var connection = await _dataSource.OpenConnectionAsync();
            await connection.ExecuteAsync(sql, new
            {
                BatchId = batchId,
                Status = BatchStatus.Delayed,
                DelayedUntil = delayedUntil,
                RetryAttempts = retryAttempts
            });
            _logger.LogInformation("Set batch {BatchId} to delayed status until {DelayedUntil}, retry attempt {RetryAttempts}",
                batchId, delayedUntil, retryAttempts);
        }

        public async Task SetManualInterventionAsync(int batchId, bool required, string? errorMessage = null)
        {
            const string sql = @"
                UPDATE batch_states
                SET status = CASE WHEN @Required THEN @StatusManual ELSE status END,
                    manual_intervention_required = @Required,
                    error_message = COALESCE(@ErrorMessage, error_message),
                    updated_at = CURRENT_TIMESTAMP
                WHERE id = @BatchId";

            await using var connection = await _dataSource.OpenConnectionAsync();
            await connection.ExecuteAsync(sql, new
            {
                BatchId = batchId,
                Required = required,
                StatusManual = BatchStatus.ManualIntervention,
                ErrorMessage = errorMessage
            });
            _logger.LogWarning("Set manual intervention for batch {BatchId}: {Required}", batchId, required);
        }

        public async Task<IEnumerable<BatchState>> GetReadyToResumeAsync()
        {
            const string sql = @"
                SELECT 
                    id as Id,
                    batch_date as BatchDate,
                    status as Status,
                    total_pages as TotalPages,
                    processed_pages as ProcessedPages,
                    failed_pages as FailedPages,
                    registration_phase as RegistrationPhase,
                    image_download_phase as ImageDownloadPhase,
                    delayed_until as DelayedUntil,
                    retry_attempts as RetryAttempts,
                    manual_intervention_required as ManualInterventionRequired,
                    auto_resume_enabled as AutoResumeEnabled,
                    error_message as ErrorMessage,
                    created_at as CreatedAt,
                    updated_at as UpdatedAt
                FROM batch_states
                WHERE status = @DelayedStatus
                  AND delayed_until <= CURRENT_TIMESTAMP
                  AND auto_resume_enabled = true
                  AND manual_intervention_required = false";

            await using var connection = await _dataSource.OpenConnectionAsync();
            var results = await connection.QueryAsync<BatchState>(sql, new { DelayedStatus = BatchStatus.Delayed });
            return results;
        }

        public async Task<IEnumerable<BatchPageError>> GetUnresolvedErrorsAsync(int batchId)
        {
            const string sql = @"
                SELECT 
                    id as Id,
                    batch_id as BatchId,
                    page_number as PageNumber,
                    phase as Phase,
                    error_type as ErrorType,
                    error_message as ErrorMessage,
                    retry_count as RetryCount,
                    last_retry_at as LastRetryAt,
                    resolved as Resolved,
                    resolved_at as ResolvedAt,
                    created_at as CreatedAt,
                    updated_at as UpdatedAt
                FROM batch_page_errors
                WHERE batch_id = @BatchId AND resolved = false
                ORDER BY page_number, phase";

            await using var connection = await _dataSource.OpenConnectionAsync();
            var results = await connection.QueryAsync<BatchPageError>(sql, new { BatchId = batchId });
            return results;
        }

        public async Task RecordPageErrorAsync(BatchPageError error)
        {
            const string sql = @"
                INSERT INTO batch_page_errors (
                    batch_id, page_number, phase, error_type, error_message,
                    retry_count, last_retry_at, resolved, created_at, updated_at
                ) VALUES (
                    @BatchId, @PageNumber, @Phase, @ErrorType, @ErrorMessage,
                    @RetryCount, @LastRetryAt, false, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
                )
                ON CONFLICT (batch_id, page_number, phase) DO UPDATE SET
                    error_type = EXCLUDED.error_type,
                    error_message = EXCLUDED.error_message,
                    retry_count = EXCLUDED.retry_count,
                    last_retry_at = EXCLUDED.last_retry_at,
                    updated_at = CURRENT_TIMESTAMP";

            await using var connection = await _dataSource.OpenConnectionAsync();
            await connection.ExecuteAsync(sql, new
            {
                error.BatchId,
                error.PageNumber,
                error.Phase,
                error.ErrorType,
                error.ErrorMessage,
                error.RetryCount,
                LastRetryAt = error.LastRetryAt ?? DateTime.UtcNow
            });
            _logger.LogWarning("Recorded error for batch {BatchId}, page {PageNumber}, phase {Phase}: {ErrorType}",
                error.BatchId, error.PageNumber, error.Phase, error.ErrorType);
        }

        public async Task MarkErrorsAsResolvedAsync(int batchId, IEnumerable<int> pageNumbers, string phase)
        {
            const string sql = @"
                UPDATE batch_page_errors
                SET resolved = true,
                    resolved_at = CURRENT_TIMESTAMP,
                    updated_at = CURRENT_TIMESTAMP
                WHERE batch_id = @BatchId
                  AND page_number = ANY(@PageNumbers)
                  AND phase = @Phase";

            var pageNumberArray = pageNumbers.ToArray();
            await using var connection = await _dataSource.OpenConnectionAsync();
            await connection.ExecuteAsync(sql, new { BatchId = batchId, PageNumbers = pageNumberArray, Phase = phase });
            _logger.LogInformation("Marked {Count} errors as resolved for batch {BatchId}, phase {Phase}",
                pageNumberArray.Length, batchId, phase);
        }

        public async Task DeletePageErrorsAsync(int batchId, IEnumerable<int> pageNumbers, string phase)
        {
            const string sql = @"
                DELETE FROM batch_page_errors
                WHERE batch_id = @BatchId
                  AND page_number = ANY(@PageNumbers)
                  AND phase = @Phase";

            var pageNumberArray = pageNumbers.ToArray();
            await using var connection = await _dataSource.OpenConnectionAsync();
            await connection.ExecuteAsync(sql, new { BatchId = batchId, PageNumbers = pageNumberArray, Phase = phase });
            _logger.LogInformation("Deleted {Count} page errors for batch {BatchId}, phase {Phase}",
                pageNumberArray.Length, batchId, phase);
        }
    }
}
