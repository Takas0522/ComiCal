using System;

namespace ComiCal.Shared.Models
{
    /// <summary>
    /// Represents a page-level error during batch processing
    /// </summary>
    public class BatchPageError
    {
        public int Id { get; set; }
        public int BatchId { get; set; }
        public int PageNumber { get; set; }
        public string Phase { get; set; } = string.Empty;
        public string ErrorType { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public int RetryCount { get; set; }
        public DateTime? LastRetryAt { get; set; }
        public bool Resolved { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Phase type constants for error tracking
    /// </summary>
    public static class BatchPhase
    {
        public const string Registration = "registration";
        public const string ImageDownload = "image_download";
    }
}
