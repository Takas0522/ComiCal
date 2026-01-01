using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ComiCal.Batch.Util
{
    /// <summary>
    /// Helper class for API Key authentication
    /// </summary>
    public class ApiKeyAuthenticationHelper
    {
        private const string ApiKeyHeaderName = "X-API-Key";
        private const string ApiKeyQueryParameterName = "api_key";
        private const string ApiKeyConfigurationKey = "BATCH_API_KEY";

        /// <summary>
        /// Validate API key from request header or query parameter
        /// </summary>
        /// <param name="req">HTTP request data</param>
        /// <param name="configuration">Configuration to retrieve the expected API key</param>
        /// <param name="logger">Logger for security audit</param>
        /// <returns>True if authentication successful, false otherwise</returns>
        public static bool ValidateApiKey(HttpRequestData req, IConfiguration configuration, ILogger logger)
        {
            try
            {
                // Get expected API key from configuration
                var expectedApiKey = configuration[ApiKeyConfigurationKey];

                // If no API key is configured, log warning and deny access
                if (string.IsNullOrWhiteSpace(expectedApiKey))
                {
                    logger.LogWarning("API Key authentication failed: No API key configured in environment");
                    return false;
                }

                // Try to get API key from header first
                string? providedApiKey = null;
                if (req.Headers.TryGetValues(ApiKeyHeaderName, out var headerValues))
                {
                    providedApiKey = headerValues.FirstOrDefault();
                }

                // If not in header, try query parameter
                if (string.IsNullOrWhiteSpace(providedApiKey))
                {
                    var queryParams = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
                    providedApiKey = queryParams[ApiKeyQueryParameterName];
                }

                // Validate API key
                if (string.IsNullOrWhiteSpace(providedApiKey))
                {
                    logger.LogWarning(
                        "API Key authentication failed: No API key provided. IP: {IP}, Path: {Path}",
                        GetClientIp(req),
                        req.Url.AbsolutePath);
                    return false;
                }

                // Constant time comparison to prevent timing attacks
                bool isValid = CryptographicEquals(providedApiKey, expectedApiKey);

                if (!isValid)
                {
                    logger.LogWarning(
                        "API Key authentication failed: Invalid API key provided. IP: {IP}, Path: {Path}",
                        GetClientIp(req),
                        req.Url.AbsolutePath);
                    return false;
                }

                logger.LogInformation(
                    "API Key authentication successful. IP: {IP}, Path: {Path}",
                    GetClientIp(req),
                    req.Url.AbsolutePath);

                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during API Key authentication");
                return false;
            }
        }

        /// <summary>
        /// Create unauthorized response
        /// </summary>
        public static async Task<HttpResponseData> CreateUnauthorizedResponseAsync(HttpRequestData req)
        {
            var response = req.CreateResponse(HttpStatusCode.Unauthorized);
            await response.WriteAsJsonAsync(new
            {
                error = "Unauthorized",
                message = "Invalid or missing API key. Provide API key via X-API-Key header or api_key query parameter.",
                timestamp = DateTime.UtcNow
            });
            return response;
        }

        /// <summary>
        /// Get client IP address from request
        /// </summary>
        private static string GetClientIp(HttpRequestData req)
        {
            // Try to get real IP from X-Forwarded-For header (for proxies/load balancers)
            if (req.Headers.TryGetValues("X-Forwarded-For", out var forwardedValues))
            {
                var forwardedIp = forwardedValues.FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(forwardedIp))
                {
                    return forwardedIp.Split(',')[0].Trim();
                }
            }

            // Try X-Real-IP header
            if (req.Headers.TryGetValues("X-Real-IP", out var realIpValues))
            {
                var realIp = realIpValues.FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(realIp))
                {
                    return realIp;
                }
            }

            return "Unknown";
        }

        /// <summary>
        /// Constant time string comparison to prevent timing attacks
        /// </summary>
        private static bool CryptographicEquals(string a, string b)
        {
            if (a == null || b == null)
            {
                return false;
            }

            if (a.Length != b.Length)
            {
                return false;
            }

            var result = 0;
            for (int i = 0; i < a.Length; i++)
            {
                result |= a[i] ^ b[i];
            }

            return result == 0;
        }
    }
}
