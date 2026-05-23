using System.Diagnostics;

namespace UmbracoMonitoring.Telemetry;

public static class DemoTelemetry
{
    public const string ActivitySourceName = "UmbracoMonitoring";

    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);
}
