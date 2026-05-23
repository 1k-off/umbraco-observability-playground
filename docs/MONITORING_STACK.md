# Observability stack

Target **Grafana LGTM+** setup with **Alloy**, **Faro**, and optionally **Beyla** for the Umbraco + Next.js demo on Kubernetes.

---

## Components

| Component | Role | Protocol / interface |
|-----------|------|----------------------|
| **Grafana** | UI, dashboards, Explore, correlation | HTTP |
| **Mimir** | Long-term metrics storage | Prometheus remote_write, PromQL |
| **Loki** | Log aggregation | LogQL push (Alloy) |
| **Tempo** | Distributed tracing | OTLP gRPC/HTTP |
| **Pyroscope** | Continuous profiling | HTTP push / scrape |
| **Grafana Alloy** | Collector: scrape, OTLP, logs, Faro | River config |
| **Grafana Faro** | RUM: Web Vitals, errors, sessions | Browser SDK → collector |
| **Beyla** (opt.) | eBPF auto HTTP traces | OTLP → Tempo |

---

## Pipeline (target diagram)

```mermaid
flowchart LR
  subgraph Apps
    Web[Next.js]
    CMS[Umbraco]
    Browser[Browser Faro]
  end
  subgraph Alloy
    OTLP[otelcol.receiver.otlp]
    Scrape[prometheus.scrape]
    Logs[loki.source.kubernetes]
    FaroR[faro.receiver]
    RW[prometheus.remote_write]
    LT[loki.write]
    TT[otelcol.exporter.otlp tempo]
    PR[pyroscope.write]
  end
  subgraph Storage
    Mimir[Mimir]
    Loki[Loki]
    Tempo[Tempo]
    Pyro[Pyroscope]
  end
  Grafana[Grafana]
  Web --> OTLP
  CMS --> OTLP
  Web --> Logs
  CMS --> Logs
  Browser --> FaroR
  Scrape --> Web
  Scrape --> CMS
  OTLP --> TT
  OTLP --> Mimir
  Logs --> LT
  FaroR --> Loki
  FaroR --> Tempo
  RW --> Mimir
  LT --> Loki
  TT --> Tempo
  Web --> PR
  CMS --> PR
  Mimir --> Grafana
  Loki --> Grafana
  Tempo --> Grafana
  Pyro --> Grafana
```

---

## Signals per service

### `umbraco-cms` (.NET)

| Signal | Source | Phase |
|--------|--------|-------|
| RED metrics | OTel → Mimir | 6 |
| ASP.NET request duration | OTel | 6 |
| SQL client duration | OTel | 6 |
| Logs | stdout JSON → Loki | 1 basic, 6 correlated |
| Traces | OTLP | 6 |
| Profiles | Pyroscope .NET SDK | 6 |
| eBPF HTTP | Beyla | 8 |

**Key spans:** Delivery API fetch, Search (Examine), DB query.

### `umbraco-web` (Node)

| Signal | Source | Phase |
|--------|--------|-------|
| RED metrics | OTel | 6 |
| SSR / route handler duration | OTel | 6 |
| CMS upstream HTTP | OTel HttpClient child span | 6 |
| Logs | stdout → Loki | 2+ when app exists, 6 correlated |
| Traces | OTLP | 6 |
| Profiles | Pyroscope Node | 6 |
| RUM | Faro | 6 |
| eBPF HTTP | Beyla | 8 |

**Key spans:** `GET /articles`, `fetch CMS`, render.

### Browser (Faro)

| Signal | Purpose |
|--------|---------|
| Web Vitals (LCP, INP, CLS) | UX under load |
| `faro.api` errors | JS/network failures |
| Session / view | Demo funnel |
| Trace link | W3C traceparent → Tempo |

---

## Labels and naming (required)

Consistent labels for Grafana correlation:

| Label | Example | Where |
|-------|---------|-------|
| `service.name` | `umbraco-cms`, `umbraco-web` | OTel resource |
| `service.namespace` | `demo-services` | OTel resource |
| `deployment.environment` | `demo` | OTel resource |
| `k8s.pod.name` | from downward API | Alloy logs |
| `http.route` | normalized template | OTel span |

**Do not use** high-cardinality labels: `user.id`, raw `article.slug` in metrics.

---

## Alloy (configuration — Phase 5 guide)

Files: `observability/alloy/config.alloy` (or ConfigMap `alloy-config`).

Minimum components:

1. `prometheus.scrape` — pods with `prometheus.io/scrape: "true"`
2. `prometheus.remote_write` → Mimir gateway URL
3. `otelcol.receiver.otlp` `0.0.0.0:4317` / `4318`
4. `otelcol.processor.batch` → `otelcol.exporter.otlp` (Tempo)
5. `loki.source.kubernetes` or `discovery.kubernetes` + `loki.source.podlogs`
6. `loki.write` → Loki gateway
7. `faro.receiver` (if not using a separate Faro collector)
8. `pyroscope.write` → Pyroscope

Gateway Service: `alloy-gateway.observability.svc:4317` (document in [RUNBOOK_OBSERVABILITY.md](./RUNBOOK_OBSERVABILITY.md)).

---

## Grafana datasources

| Datasource | UID (recommended) | Type |
|------------|-------------------|------|
| Mimir | `mimir` | Prometheus |
| Loki | `loki` | Loki |
| Tempo | `tempo` | Tempo |
| Pyroscope | `pyroscope` | Grafana Pyroscope |
| Faro | `faro` | Grafana Faro |

Enable **correlations**: Loki → Tempo (trace_id), Tempo → Loki, Tempo → Pyroscope.

---

## Sampling and retention (demo)

| Parameter | Value |
|-----------|-------|
| Trace sampling | 100% smoke; 10–20% load test |
| Mimir metrics retention | 7d |
| Loki retention | 7d |
| Log volume | JSON info; debug off in demo |
| Profiles | 15s scrape / push interval |

---

## Dashboards (Phase 7 — list)

1. **Overview** — RPS, error rate, p95 latency (web + cms)
2. **Umbraco CMS** — search duration, SQL, slow endpoint
3. **Next.js Web** — route latency, CMS fetch child spans
4. **Faro RUM** — Vitals, JS errors, sessions
5. **k6** (optional) — if metrics from k6 output
6. **Infrastructure** — pod CPU/mem, HPA

Store JSON in `observability/grafana/dashboards/`.

---

## Alerts (minimum for demo)

| Alert | Condition | Demo purpose |
|-------|-----------|--------------|
| HighErrorRate | 5xx > 1% for 5m | fault endpoint |
| HighLatency | p95 > 1s for 5m | slow endpoint + k6 |
| LogErrorBurst | Loki `level=error` rate | incident |

---

## Beyla vs OTel (Phase 8)

| | OTel SDK | Beyla |
|---|----------|-------|
| Invasiveness | Code + deps | Annotations / DaemonSet |
| Detail | In-app spans | HTTP/K8s level |
| Use | Primary path | Comparison / supplement |

Document in `docs/BEYLA_VS_OTEL.md` after Phase 8.

---

## Kubernetes deployment

- Namespace: `observability`
- Preference: official Grafana Helm charts / `grafana/k8s-monitoring-helm`
- `deployment/k8s/observability/README.md` — chart versions, values overrides, `helm install` order

**Do not** mix observability manifests with `deployment/k8s/apps/` — different lifecycles.

---

## Stack readiness checklist

- [ ] PromQL: `http_server_request_duration_seconds` (or OTel metric name) for both services
- [ ] LogQL: `{service_name="umbraco-web"} |= "error"`
- [ ] TraceQL: `{ resource.service.name="umbraco-web" }`
- [ ] Pyroscope: flame graph under k6 load
- [ ] Faro: sessions > 0 during k6 browser scenario (if used)
