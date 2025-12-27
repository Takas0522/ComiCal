using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ComiCal.Shared.Util
{
    public static class ImageUrlHelper
    {
        public static string GetImageUrl(string blobBaseUrl, string isbn, string extension)
        {
            if (string.IsNullOrWhiteSpace(blobBaseUrl))
            {
                throw new ArgumentException("blobBaseUrl cannot be null or whitespace.", nameof(blobBaseUrl));
            }

            if (string.IsNullOrWhiteSpace(isbn))
            {
                throw new ArgumentException("isbn cannot be null or whitespace.", nameof(isbn));
            }

            if (string.IsNullOrWhiteSpace(extension))
            {
                throw new ArgumentException("extension cannot be null or whitespace.", nameof(extension));
            }

            // Remove leading slash from blobBaseUrl if present to avoid double slash
            var baseUrl = blobBaseUrl.TrimEnd('/');
            
            // Build the image URL with the format: {baseUrl}/images/{isbn}.{extension}
            return $"{baseUrl}/images/{isbn}.{extension}";
        }
    }
}
