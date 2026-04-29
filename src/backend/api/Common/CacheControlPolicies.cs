namespace ComiCal.Api.Common;

/// <summary>
/// Cache-Control header presets for HTTP responses. Only anonymous-safe
/// (non user-scoped) GET endpoints should set these — never on /me/* or any
/// authenticated read where the payload differs per user.
/// </summary>
internal static class CacheControlPolicies
{
    /// <summary>
    /// 60-second freshness with 5-minute stale-while-revalidate window. Suitable
    /// for catalog reads (series search/detail, calendar, volume search/upcoming)
    /// where eventual consistency is fine and traffic spikes are common after
    /// the daily 03:00 JST batch publishes new volumes.
    /// </summary>
    public const string AnonymousCatalog = "public, max-age=60, stale-while-revalidate=300";
}
