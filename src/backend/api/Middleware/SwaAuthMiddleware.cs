using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using ComiCal.Api.Common;
using ComiCal.Api.ProblemDetails;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ComiCal.Api.Middleware;

/// <summary>
/// Decodes the <c>x-ms-client-principal</c> header (base64-encoded JSON)
/// emitted by Azure Static Web Apps' linked-Functions integration and exposes
/// the resulting <see cref="ClientPrincipal"/> through the scoped
/// <see cref="ICurrentUserAccessor"/>.
///
/// Behaviour:
/// <list type="bullet">
///   <item>No header → <see cref="ClientPrincipal.Anonymous"/>.</item>
///   <item>Malformed header (not base64 / not JSON) → 400 problem+json.</item>
///   <item>Function annotated with <see cref="RequiresAuthenticatedUserAttribute"/>
///         called without an authenticated principal → 401 problem+json.</item>
/// </list>
/// </summary>
public sealed class SwaAuthMiddleware(
    ILogger<SwaAuthMiddleware> logger,
    ProblemDetailsFactory problemFactory) : IFunctionsWorkerMiddleware
{
    /// <summary>SWA-injected header name; case-insensitive on incoming.</summary>
    public const string HeaderName = "x-ms-client-principal";

    /// <summary>Key used to expose the decoded principal on <see cref="FunctionContext.Items"/>.</summary>
    public const string ItemKey = "__comical.clientPrincipal";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ILogger<SwaAuthMiddleware> _logger = logger;
    private readonly ProblemDetailsFactory _problemFactory = problemFactory;

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        ClientPrincipal principal;
        try
        {
            principal = ExtractPrincipal(context) ?? ClientPrincipal.Anonymous;
        }
        catch (FormatException ex)
        {
            _logger.LogWarning(ex, "Malformed x-ms-client-principal header on {InvocationId}", context.InvocationId);
            await WriteProblemAsync(context, HttpStatusCode.BadRequest,
                _problemFactory.FromError(
                    ComiCal.Shared.Error.Validation("invalid-client-principal", "x-ms-client-principal header was not valid base64 JSON."),
                    instance: ResolveInstance(context),
                    traceId: ResolveTraceId(context))).ConfigureAwait(false);
            return;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Malformed x-ms-client-principal JSON on {InvocationId}", context.InvocationId);
            await WriteProblemAsync(context, HttpStatusCode.BadRequest,
                _problemFactory.FromError(
                    ComiCal.Shared.Error.Validation("invalid-client-principal", "x-ms-client-principal header JSON could not be parsed."),
                    instance: ResolveInstance(context),
                    traceId: ResolveTraceId(context))).ConfigureAwait(false);
            return;
        }

        context.Items[ItemKey] = principal;

        var accessor = context.InstanceServices.GetService<ICurrentUserAccessor>();
        if (accessor is CurrentUserAccessor concrete)
        {
            concrete.Principal = principal;
        }

        if (RequiresAuthentication(context) && !principal.IsAuthenticated)
        {
            _logger.LogInformation("Anonymous access denied on {InvocationId} (route requires authentication)", context.InvocationId);
            await WriteProblemAsync(context, HttpStatusCode.Unauthorized,
                _problemFactory.FromError(
                    new ComiCal.Shared.Error(ComiCal.Shared.ErrorKind.Unauthorized, "unauthorized", "Authentication is required."),
                    instance: ResolveInstance(context),
                    traceId: ResolveTraceId(context))).ConfigureAwait(false);
            return;
        }

        await next(context).ConfigureAwait(false);
    }

    private static ClientPrincipal? ExtractPrincipal(FunctionContext context)
    {
        var http = context.GetHttpContext();
        if (http is null)
        {
            return null;
        }

        if (!http.Request.Headers.TryGetValue(HeaderName, out var values) || values.Count == 0)
        {
            return null;
        }

        return DecodePrincipalHeader(values[0]);
    }

    /// <summary>
    /// Decodes a base64-encoded SWA <c>x-ms-client-principal</c> header value.
    /// Returns <c>null</c> for null/empty input. Throws <see cref="FormatException"/> on bad base64
    /// and <see cref="JsonException"/> on bad JSON; callers map those to RFC 7807 400.
    /// Exposed for unit testing.
    /// </summary>
    public static ClientPrincipal? DecodePrincipalHeader(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var bytes = Convert.FromBase64String(raw);
        var json = Encoding.UTF8.GetString(bytes);
        var dto = JsonSerializer.Deserialize<ClientPrincipalDto>(json, JsonOptions);
        if (dto is null)
        {
            return null;
        }

        var roles = (IReadOnlyList<string>?)dto.UserRoles ?? Array.Empty<string>();
        var claims = (dto.Claims ?? new List<ClientPrincipalClaimDto>())
            .Select(c => new ClientPrincipalClaim(c.Typ ?? c.Type ?? string.Empty, c.Val ?? c.Value ?? string.Empty))
            .ToArray();

        return new ClientPrincipal(
            IdentityProvider: dto.IdentityProvider ?? string.Empty,
            UserId: dto.UserId ?? string.Empty,
            UserDetails: dto.UserDetails ?? string.Empty,
            UserRoles: roles,
            Claims: claims);
    }

    /// <summary>
    /// Returns <c>true</c> if <paramref name="declaringType"/> or <paramref name="methodName"/>
    /// is annotated with <see cref="RequiresAuthenticatedUserAttribute"/>. Exposed for unit tests.
    /// </summary>
    public static bool TypeOrMethodRequiresAuth(Type declaringType, string methodName)
    {
        ArgumentNullException.ThrowIfNull(declaringType);
        if (declaringType.GetCustomAttribute<RequiresAuthenticatedUserAttribute>(inherit: true) is not null)
        {
            return true;
        }

        var method = declaringType
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
            .FirstOrDefault(m => string.Equals(m.Name, methodName, StringComparison.Ordinal));
        return method?.GetCustomAttribute<RequiresAuthenticatedUserAttribute>(inherit: true) is not null;
    }

    private static bool RequiresAuthentication(FunctionContext context)
    {
        var entry = context.FunctionDefinition.EntryPoint; // "Namespace.Type.Method"
        var lastDot = entry.LastIndexOf('.');
        if (lastDot <= 0)
        {
            return false;
        }

        var typeName = entry.Substring(0, lastDot);
        var methodName = entry.Substring(lastDot + 1);

        // Search loaded assemblies for the declaring type.
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type? t;
            try
            {
                t = asm.GetType(typeName, throwOnError: false);
            }
            catch
            {
                continue;
            }

            if (t is null)
            {
                continue;
            }

            if (t.GetCustomAttribute<RequiresAuthenticatedUserAttribute>(inherit: true) is not null)
            {
                return true;
            }

            var method = t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                .FirstOrDefault(m => string.Equals(m.Name, methodName, StringComparison.Ordinal));
            if (method?.GetCustomAttribute<RequiresAuthenticatedUserAttribute>(inherit: true) is not null)
            {
                return true;
            }

            return false;
        }

        return false;
    }

    private static async Task WriteProblemAsync(FunctionContext context, HttpStatusCode status, ProblemDetailsBody body)
    {
        var http = context.GetHttpContext();
        if (http is null || http.Response.HasStarted)
        {
            return;
        }

        http.Response.StatusCode = (int)status;
        http.Response.ContentType = "application/problem+json";
        await JsonSerializer.SerializeAsync(http.Response.Body, body, JsonOptions, http.RequestAborted).ConfigureAwait(false);
    }

    private static string? ResolveInstance(FunctionContext context)
        => context.GetHttpContext()?.Request.Path.Value;

    private static string? ResolveTraceId(FunctionContext context)
        => CorrelationContextAccessor.GetCorrelationId(context) ?? context.InvocationId;

    // ---------------- DTOs (matches https://learn.microsoft.com/azure/static-web-apps/user-information) ----------------

    private sealed class ClientPrincipalDto
    {
        [JsonPropertyName("identityProvider")] public string? IdentityProvider { get; set; }
        [JsonPropertyName("userId")] public string? UserId { get; set; }
        [JsonPropertyName("userDetails")] public string? UserDetails { get; set; }
        [JsonPropertyName("userRoles")] public List<string>? UserRoles { get; set; }
        [JsonPropertyName("claims")] public List<ClientPrincipalClaimDto>? Claims { get; set; }
    }

    private sealed class ClientPrincipalClaimDto
    {
        // SWA uses {typ,val}; some samples use {type,value}. Accept either.
        [JsonPropertyName("typ")] public string? Typ { get; set; }
        [JsonPropertyName("val")] public string? Val { get; set; }
        [JsonPropertyName("type")] public string? Type { get; set; }
        [JsonPropertyName("value")] public string? Value { get; set; }
    }
}
