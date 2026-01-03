using System;

namespace ComiCal.Shared.Models
{
    /// <summary>
    /// Represents the state of a batch execution for a specific date
    /// </summary>
    public class BatchState
    {
        public int Id { get; set; }
        public DateTime BatchDate { get; set; }
        public string Status { get; set; } = "pending";
        public int? TotalPages { get; set; }
        public int ProcessedPages { get; set; }
        public int FailedPages { get; set; }
        public string RegistrationPhase { get; set; } = "pending";
        public string ImageDownloadPhase { get; set; } = "pending";
        public DateTime? DelayedUntil { get; set; }
        public int RetryAttempts { get; set; }
        public bool ManualInterventionRequired { get; set; }
        public bool AutoResumeEnabled { get; set; } = true;
        public string? ErrorMessage { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Batch status constants
    /// </summary>
    public static class BatchStatus
    {
        public const string Pending = "pending";
        public const string Running = "running";
        public const string Completed = "completed";
        public const string Failed = "failed";
        public const string Delayed = "delayed";
        public const string ManualIntervention = "manual_intervention";
    }

    /// <summary>
    /// Phase status constants
    /// </summary>
    public static class PhaseStatus
    {
        public const string Pending = "pending";
        public const string Running = "running";
        public const string Completed = "completed";
        public const string Failed = "failed";
    }
}
