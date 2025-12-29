using System;

namespace Comical.Api.Util.Common
{
    /// <summary>
    /// Represents an error response returned from API endpoints.
    /// </summary>
    public class ErrorResponse
    {
        /// <summary>
        /// Initializes a new instance of the ErrorResponse class.
        /// </summary>
        public ErrorResponse()
        {
            Timestamp = DateTime.UtcNow;
        }

        /// <summary>
        /// Gets or sets the error message.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the error details (optional).
        /// </summary>
        public string? Details { get; set; }

        /// <summary>
        /// Gets the timestamp when the error occurred.
        /// </summary>
        public DateTime Timestamp { get; }
    }
}
