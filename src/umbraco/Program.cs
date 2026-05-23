using System.IO.Compression;
using HealthChecks.SqlServer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using UmbracoMonitoring.Configuration;
using UmbracoMonitoring.Middleware;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

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
    options.KnownNetworks.Clear();
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
