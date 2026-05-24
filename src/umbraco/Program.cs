using System.IO.Compression;
using HealthChecks.SqlServer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;
using Serilog.Events;
using UmbracoMonitoring.Configuration;
using UmbracoMonitoring.Extensions;
using UmbracoMonitoring.Logging;
using UmbracoMonitoring.Middleware;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, _, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.With<TraceLogEnricher>());

builder.AddDemoObservability();

builder.Services.AddControllers();
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = false;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});
builder.Services.Configure<DemoOptions>(builder.Configuration.GetSection(DemoOptions.SectionName));
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

string? connectionString = builder.Configuration.GetConnectionString("umbracoDbDSN");
if (!string.IsNullOrWhiteSpace(connectionString))
{
    builder.Services.AddHealthChecks()
        .AddSqlServer(connectionString, name: "sqlserver");
}

builder.CreateUmbracoBuilder()
    .AddBackOffice()
    .AddDeliveryApi()
    .AddComposers()
    .Build();

WebApplication app = builder.Build();

app.UseForwardedHeaders();
app.UseSerilogRequestLogging(options =>
{
    options.GetLevel = static (context, _, exception) =>
    {
        if (!ProbePathFilters.ShouldObserveHttpRequest(context.Request.Path))
            return LogEventLevel.Verbose;

        return exception is not null ? LogEventLevel.Error : LogEventLevel.Information;
    };
});
app.UseMiddleware<RequestIdMiddleware>();
app.UseResponseCompression();

app.MapHealthChecks("/health");
app.MapGet("/ready", async (HealthCheckService health) =>
{
    HealthReport report = await health.CheckHealthAsync();
    return report.Status == HealthStatus.Healthy
        ? Results.Ok(new { status = "ready" })
        : Results.Json(new { status = "not ready", report.Status }, statusCode: StatusCodes.Status503ServiceUnavailable);
});

await app.BootUmbracoAsync();

app.MapControllers();

app.UseUmbraco()
    .WithMiddleware(u =>
    {
        u.UseBackOffice();
    })
    .WithEndpoints(u =>
    {
        u.UseBackOfficeEndpoints();
        u.UseDeliveryApiEndpoints();
    });

await app.RunAsync();
