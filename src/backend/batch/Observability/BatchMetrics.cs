using Microsoft.ApplicationInsights;

namespace ComiCal.Batch.Observability;

/// <summary>
/// Names of the high-value custom metrics emitted from Batch activities.
/// Auto-collected counters (<c>requests/duration</c>, <c>dependencies/duration</c>) are NOT
/// duplicated here — only metrics that need explicit dimensions or business meaning.
/// </summary>
/// <remarks>
/// Per docs/specs/oo-init/14-observability-sre.md §14.1 these feed the Daily Batch Workbook
/// and several alert rules in <c>infra/modules/observability.bicep</c>.
/// </remarks>
public static class BatchMetricNames
{
    /// <summary>Counter: one increment per outbound Rakuten Books API call. Dimension: <c>endpoint</c>.</summary>
    public const string RakutenApiCalls = "rakuten.api.calls";

    /// <summary>Counter: one increment whenever the Rakuten Books API responds with HTTP 429.</summary>
    public const string RakutenApiRateLimited = "rakuten.api.rate_limited";

    /// <summary>Gauge: number of volumes successfully ingested in a single orchestration run.</summary>
    public const string BatchVolumesIngested = "batch.volumes_ingested";

    /// <summary>Histogram: end-to-end orchestration duration in seconds.</summary>
    public const string BatchDurationSeconds = "batch.duration_seconds";
}

/// <summary>
/// Thin wrapper over <see cref="TelemetryClient.GetMetric(string, string)"/> that fans out
/// to pre-named custom metrics. Activities take an <see cref="IBatchMetrics"/> dependency so
/// the call sites read declaratively (and so unit tests can substitute a no-op).
/// </summary>
public interface IBatchMetrics
{
    /// <summary>Records a single Rakuten Books API call against the given endpoint.</summary>
    void RecordRakutenCall(string endpoint);

    /// <summary>Records a Rakuten Books HTTP 429 (rate-limit) response.</summary>
    void RecordRakutenRateLimited(string endpoint);

    /// <summary>Records the volume-count outcome of a finished orchestration run.</summary>
    void RecordVolumesIngested(int count);

    /// <summary>Records the wall-clock duration of a finished orchestration run.</summary>
    void RecordOrchestrationDuration(TimeSpan duration);
}

/// <inheritdoc cref="IBatchMetrics"/>
public sealed class BatchMetrics(TelemetryClient telemetryClient) : IBatchMetrics
{
    private readonly TelemetryClient _client = telemetryClient;

    /// <inheritdoc />
    public void RecordRakutenCall(string endpoint)
        => _client.GetMetric(BatchMetricNames.RakutenApiCalls, "endpoint")
                  .TrackValue(1, endpoint ?? "unknown");

    /// <inheritdoc />
    public void RecordRakutenRateLimited(string endpoint)
        => _client.GetMetric(BatchMetricNames.RakutenApiRateLimited, "endpoint")
                  .TrackValue(1, endpoint ?? "unknown");

    /// <inheritdoc />
    public void RecordVolumesIngested(int count)
        => _client.GetMetric(BatchMetricNames.BatchVolumesIngested).TrackValue(count);

    /// <inheritdoc />
    public void RecordOrchestrationDuration(TimeSpan duration)
        => _client.GetMetric(BatchMetricNames.BatchDurationSeconds).TrackValue(duration.TotalSeconds);
}
