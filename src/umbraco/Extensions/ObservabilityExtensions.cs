using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Pyroscope.OpenTelemetry;
using UmbracoMonitoring.Configuration;
using UmbracoMonitoring.Telemetry;

namespace UmbracoMonitoring.Extensions;

public static class ObservabilityExtensions
{
    public static IHostApplicationBuilder AddDemoObservability(this IHostApplicationBuilder builder)
    {
        ObservabilityOptions options = builder.Configuration
            .GetSection(ObservabilityOptions.SectionName)
            .Get<ObservabilityOptions>() ?? new ObservabilityOptions();

        builder.Services.Configure<ObservabilityOptions>(
            builder.Configuration.GetSection(ObservabilityOptions.SectionName));
        builder.Services.AddSingleton(DemoTelemetry.ActivitySource);

        Uri? otlpEndpoint = string.IsNullOrWhiteSpace(options.Otlp.Endpoint)
            ? null
            : new Uri(options.Otlp.Endpoint);

        OtlpExportProtocol otlpProtocol = string.Equals(
            options.Otlp.Protocol,
            "http/protobuf",
            StringComparison.OrdinalIgnoreCase)
            ? OtlpExportProtocol.HttpProtobuf
            : OtlpExportProtocol.Grpc;

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(
                    serviceName: options.ServiceName,
                    serviceNamespace: options.ServiceNamespace,
                    serviceVersion: typeof(ObservabilityExtensions).Assembly.GetName().Version?.ToString() ?? "0.0.0")
                .AddAttributes(new Dictionary<string, object>
                {
                    ["deployment.environment"] = options.DeploymentEnvironment,
                }))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation(instrumentation =>
                {
                    instrumentation.Filter = context =>
                        !IsProbePath(context.Request.Path);
                    instrumentation.RecordException = true;
                })
                .AddHttpClientInstrumentation()
                .AddSqlClientInstrumentation(sql => sql.RecordException = true)
                .AddSource(DemoTelemetry.ActivitySourceName)
                .AddProcessor(new PyroscopeSpanProcessor())
                .AddOtlpExporter(exporter =>
                {
                    if (otlpEndpoint is not null)
                    {
                        exporter.Endpoint = otlpEndpoint;
                    }

                    exporter.Protocol = otlpProtocol;
                }))
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddMeter(DemoTelemetry.ActivitySourceName)
                .AddOtlpExporter(exporter =>
                {
                    if (otlpEndpoint is not null)
                    {
                        exporter.Endpoint = otlpEndpoint;
                    }

                    exporter.Protocol = otlpProtocol;
                }));

        return builder;
    }

    private static bool IsProbePath(PathString path)
    {
        return path.StartsWithSegments("/health")
            || path.StartsWithSegments("/ready");
    }
}
