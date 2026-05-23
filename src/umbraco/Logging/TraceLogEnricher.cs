using System.Diagnostics;
using Serilog.Core;
using Serilog.Events;

namespace UmbracoMonitoring.Logging;

/// <summary>
/// Adds trace_id and span_id from <see cref="Activity.Current"/> for Loki ↔ Tempo correlation.
/// </summary>
public sealed class TraceLogEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        Activity? activity = Activity.Current;
        if (activity is null)
        {
            return;
        }

        string traceId = activity.TraceId.ToString();
        string spanId = activity.SpanId.ToString();
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("trace_id", traceId));
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("span_id", spanId));
        // PascalCase alias for LogQL/docs that used TraceId; same W3C id as Tempo.
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("TraceId", traceId));
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("SpanId", spanId));
    }
}
