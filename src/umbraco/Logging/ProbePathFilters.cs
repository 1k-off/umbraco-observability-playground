namespace UmbracoMonitoring.Logging;

/// <summary>
/// Shared rules for probes, Umbraco backoffice static UI, and other low-value HTTP traffic.
/// </summary>
internal static class ProbePathFilters
{
    public static bool IsProbePath(PathString path) =>
        path.StartsWithSegments("/health")
        || path.StartsWithSegments("/ready");

    public static bool IsUmbracoUiPath(PathString path) =>
        path.StartsWithSegments("/umbraco/lib")
        || path.StartsWithSegments("/umbraco/assets")
        || path.StartsWithSegments("/umbraco/backoffice")
        || path.StartsWithSegments("/umbraco/webservices")
        || path.StartsWithSegments("/umbraco/hub");

    /// <summary>ASP.NET request logs and OTLP HTTP spans for demo-relevant API traffic only.</summary>
    public static bool ShouldObserveHttpRequest(PathString path) =>
        !IsProbePath(path) && !IsUmbracoUiPath(path);

}
