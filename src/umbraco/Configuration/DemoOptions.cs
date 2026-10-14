namespace UmbracoMonitoring.Configuration;

public class DemoOptions
{
    public const string SectionName = "Demo";

    public bool SlowEndpoint { get; set; }

    public bool FaultEndpoint { get; set; }

    public double FaultRate { get; set; } = 0.1;

    /// <summary>
    /// Intentional N+1 article enrichment endpoint for load/observability demos.
    /// </summary>
    public bool NaiveArticlesEndpoint { get; set; }
}
