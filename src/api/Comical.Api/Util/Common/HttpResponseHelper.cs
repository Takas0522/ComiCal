using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Http;

namespace Comical.Api.Util.Common
{
    /// <summary>
    /// Helper class for creating HTTP responses in Azure Functions (Isolated worker model).
    /// </summary>
    public static class HttpResponseHelper
    {
        private const string JsonContentType = "application/json; charset=utf-8";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        /// <summary>
        /// Creates a successful (200 OK) HTTP response with JSON data.
        /// </summary>
        /// <typeparam name="T">The type of data to serialize.</typeparam>
        /// <param name="request">The HTTP request data.</param>
        /// <param name="data">The data to include in the response. Cannot be null.</param>
        /// <returns>An HTTP response with status 200 and the serialized data.</returns>
        /// <exception cref="ArgumentNullException">Thrown when data is null.</exception>
        public static async Task<HttpResponseData> CreateOkResponseAsync<T>(
            HttpRequestData request,
            T data)
        {
            if (data is null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            var response = request.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", JsonContentType);
            await response.WriteAsJsonAsync(data, JsonOptions);
            return response;
        }

        /// <summary>
        /// Creates a bad request (400) HTTP response with error information.
        /// </summary>
        /// <param name="request">The HTTP request data.</param>
        /// <param name="message">The error message.</param>
        /// <param name="details">Optional error details.</param>
        /// <returns>An HTTP response with status 400 and error information.</returns>
        public static async Task<HttpResponseData> CreateBadRequestResponseAsync(
            HttpRequestData request,
            string message,
            string? details = null)
        {
            return await CreateErrorResponseInternalAsync(
                request,
                HttpStatusCode.BadRequest,
                message,
                details);
        }

        /// <summary>
        /// Creates an internal server error (500) HTTP response with error information.
        /// </summary>
        /// <param name="request">The HTTP request data.</param>
        /// <param name="message">The error message.</param>
        /// <param name="details">Optional error details.</param>
        /// <returns>An HTTP response with status 500 and error information.</returns>
        public static async Task<HttpResponseData> CreateErrorResponseAsync(
            HttpRequestData request,
            string message,
            string? details = null)
        {
            return await CreateErrorResponseInternalAsync(
                request,
                HttpStatusCode.InternalServerError,
                message,
                details);
        }

        /// <summary>
        /// Creates an error HTTP response with the specified status code.
        /// </summary>
        private static async Task<HttpResponseData> CreateErrorResponseInternalAsync(
            HttpRequestData request,
            HttpStatusCode statusCode,
            string message,
            string? details)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(message);

            var errorResponse = new ErrorResponse
            {
                Message = message,
                Details = details
            };

            var response = request.CreateResponse(statusCode);
            response.Headers.Add("Content-Type", JsonContentType);
            await response.WriteAsJsonAsync(errorResponse, JsonOptions);
            return response;
        }
    }
}
