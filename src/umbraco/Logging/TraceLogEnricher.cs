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

        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("trace_id", activity.TraceId.ToString()));
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("span_id", activity.SpanId.ToString()));
    }
}
