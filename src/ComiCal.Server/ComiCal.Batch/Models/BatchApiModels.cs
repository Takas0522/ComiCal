using System;

namespace ComiCal.Batch.Models
{
    /// <summary>
    /// Request model for partial retry with page range
    /// </summary>
    public class PartialRetryRequest
    {
        public int StartPage { get; set; }
        public int EndPage { get; set; }
    }

    /// <summary>
    /// Request model for reset intervention
    /// </summary>
    public class ResetInterventionRequest
    {
        public int? BatchId { get; set; }
    }

    /// <summary>
    /// Response model for job execution
    /// </summary>
    public class JobExecutionResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? BatchId { get; set; }
        public string? JobType { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Response model for partial retry
    /// </summary>
    public class PartialRetryResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? BatchId { get; set; }
        public int? StartPage { get; set; }
        public int? EndPage { get; set; }
        public int? PageCount { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Response model for intervention reset
    /// </summary>
    public class ResetInterventionResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? BatchId { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Error response model
    /// </summary>
    public class ErrorResponse
    {
        public string Error { get; set; } = string.Empty;
        public string? Details { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
