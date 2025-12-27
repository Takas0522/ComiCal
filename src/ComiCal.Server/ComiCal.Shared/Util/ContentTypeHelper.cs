using System;

namespace ComiCal.Shared.Util
{
    public static class ContentTypeHelper
    {
        /// <summary>
        /// Gets the file extension from the given content type.
        /// </summary>
        /// <param name="contentType">The content type string (e.g., "image/jpeg").</param>
        /// <returns>The file extension including the leading dot (e.g., ".jpg"). Returns ".jpg" for null, empty, or unknown content types.</returns>
        public static string GetExtensionFromContentType(string contentType)
        {
            if (string.IsNullOrWhiteSpace(contentType))
            {
                return ".jpg";
            }

            // Normalize the content type to lowercase and remove any parameters (e.g., charset)
            var normalizedContentType = contentType.Trim().ToLowerInvariant();
            var semicolonIndex = normalizedContentType.IndexOf(';');
            if (semicolonIndex > 0)
            {
                normalizedContentType = normalizedContentType.Substring(0, semicolonIndex).Trim();
            }

            return normalizedContentType switch
            {
                "image/jpeg" => ".jpg",
                "image/png" => ".png",
                "image/gif" => ".gif",
                "image/webp" => ".webp",
                _ => ".jpg" // Default to .jpg for unknown content types
            };
        }
    }
}
