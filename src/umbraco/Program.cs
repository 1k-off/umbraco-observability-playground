using HealthChecks.SqlServer;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using UmbracoMonitoring.Configuration;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.Configure<DemoOptions>(builder.Configuration.GetSection(DemoOptions.SectionName));

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
