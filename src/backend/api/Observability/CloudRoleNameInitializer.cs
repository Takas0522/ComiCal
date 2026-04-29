using System.Diagnostics;
using ComiCal.Api.Common;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.DependencyInjection;

namespace ComiCal.Api.Observability;

/// <summary>
/// Stamps every outgoing telemetry item with:
/// <list type="bullet">
///   <item><description><c>cloud_RoleName = "comical-api"</c> so workbooks / Application Map can distinguish
///   the API Function App from the Batch Function App.</description></item>
///   <item><description><c>operation_Id</c> defaulted to the current <see cref="Activity"/> trace id when the
///   Functions Worker hasn't already set one (keeps SSR → API → Batch correlation intact).</description></item>
///   <item><description><c>UserId</c> custom dimension (resolved internal user id) when an authenticated
///   <see cref="ICurrentUser"/> is on the request scope. Anonymous invocations omit it; we never record
///   raw IdP subjects (PII rule per docs/specs/oo-init/14-observability-sre.md §14.3).</description></item>
/// </list>
/// </summary>
/// <remarks>
/// The initializer is registered as a singleton and resolves <see cref="ICurrentUser"/> per-call from
/// the root <see cref="IServiceProvider"/> via a created scope. Functions Isolated does not expose the
/// per-invocation scope here, so we fall back to <see cref="Activity.Current"/> baggage when no scope
/// has populated <see cref="ICurrentUser"/>. Failure to resolve a user must never throw — telemetry
/// initializers run on hot paths.
/// </remarks>
public sealed class CloudRoleNameInitializer(IServiceProvider serviceProvider) : ITelemetryInitializer
{
    /// <summary>Cloud role name applied to every API-emitted telemetry item.</summary>
    public const string RoleName = "comical-api";

    private readonly IServiceProvider _serviceProvider = serviceProvider;

    /// <inheritdoc />
    public void Initialize(ITelemetry telemetry)
    {
        ArgumentNullException.ThrowIfNull(telemetry);

        if (string.IsNullOrEmpty(telemetry.Context.Cloud.RoleName))
        {
            telemetry.Context.Cloud.RoleName = RoleName;
        }

        if (string.IsNullOrEmpty(telemetry.Context.Operation.Id))
        {
            var traceId = Activity.Current?.TraceId.ToString();
            if (!string.IsNullOrEmpty(traceId))
            {
                telemetry.Context.Operation.Id = traceId;
            }
        }

        if (telemetry is ISupportProperties props)
        {
            var userId = TryResolveUserId();
            if (userId is not null && !props.Properties.ContainsKey("UserId"))
            {
                props.Properties["UserId"] = userId;
            }
        }
    }

    private string? TryResolveUserId()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var user = scope.ServiceProvider.GetService<ICurrentUser>();
            return user is { IsAuthenticated: true } ? user.Id.ToString() : null;
        }
        catch
        {
            return null;
        }
    }
}
