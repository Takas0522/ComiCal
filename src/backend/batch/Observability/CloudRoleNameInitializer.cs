using System.Diagnostics;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;

namespace ComiCal.Batch.Observability;

/// <summary>
/// Stamps every Batch-emitted telemetry item with <c>cloud_RoleName = "comical-batch"</c>
/// and ensures <c>operation_Id</c> is populated from the current <see cref="Activity"/> trace
/// so SSR → API → Batch correlation survives across the Durable Functions hop.
/// </summary>
/// <remarks>
/// Singleton, registered in <c>Program.cs</c>. No I/O, no allocation on the hot path beyond
/// the trace-id string conversion. PII rule (docs/specs/oo-init/14-observability-sre.md §14.3)
/// applies — never stamp anonymous or IdP subject ids here.
/// </remarks>
public sealed class CloudRoleNameInitializer : ITelemetryInitializer
{
    /// <summary>Cloud role name applied to every Batch-emitted telemetry item.</summary>
    public const string RoleName = "comical-batch";

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
    }
}
