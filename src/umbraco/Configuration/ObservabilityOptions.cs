namespace UmbracoMonitoring.Configuration;

public class ObservabilityOptions
{
    public const string SectionName = "Observability";

    public string ServiceName { get; set; } = "umbraco-cms";

    public string ServiceNamespace { get; set; } = "demo-services";

    public string DeploymentEnvironment { get; set; } = "demo";

    public OtlpOptions Otlp { get; set; } = new();
}

public class OtlpOptions
{
    public string Endpoint { get; set; } = "http://localhost:4317";

    public string Protocol { get; set; } = "grpc";
}
