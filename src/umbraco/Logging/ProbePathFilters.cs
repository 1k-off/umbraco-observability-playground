namespace UmbracoMonitoring.Logging;

internal static class ProbePathFilters
{
    public static bool IsProbePath(PathString path) =>
        path.StartsWithSegments("/health")
        || path.StartsWithSegments("/ready");
}
