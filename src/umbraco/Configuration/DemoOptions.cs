namespace UmbracoMonitoring.Configuration;

public class DemoOptions
{
    public const string SectionName = "Demo";

    public bool SlowEndpoint { get; set; }

    public bool FaultEndpoint { get; set; }

    public double FaultRate { get; set; } = 0.1;
}
