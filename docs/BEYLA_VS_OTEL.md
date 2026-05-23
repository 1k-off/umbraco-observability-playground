# Beyla vs OpenTelemetry SDK

**Status:** draft — fill in after **Phase 8** when Beyla is enabled.

## Purpose

Compare eBPF-based HTTP instrumentation (Beyla) with in-app OTel spans from Phase 5.

## Comparison table (to complete)

| Aspect | OTel SDK (Phase 5) | Beyla (Phase 8) |
|--------|-------------------|-----------------|
| Setup | NuGet / npm, OTLP exporter | DaemonSet + pod annotations |
| Span detail | In-app (search, SQL, handlers) | HTTP/K8s level |
| Primary use | Demo path | Supplement / comparison |

## References

- [MONITORING_STACK.md](./MONITORING_STACK.md) — Beyla vs OTel summary
- [IMPLEMENTATION_PLAN.md](./IMPLEMENTATION_PLAN.md) — Phase 8 tasks
