# Demo script (presentation)

Phase 7 — load → dashboards → investigation (15–20 min).

## Preparation (5 min)

- [x] K8s apps: `demo-services`, Ingress CMS + web
- [x] Observability: Grafana at `https://demo-grafana.1node.tech`
- [x] Dashboards as code: `observability/grafana/dashboards/` → ConfigMaps (`scripts/apply-grafana-dashboards.ps1`); Grafana sidecar enabled
- [ ] `Demo__SlowEndpoint`, `Demo__FaultEndpoint`, `Demo__NaiveArticlesEndpoint` = false

Folders in Grafana: **Demo**

| Dashboard | UID |
|-----------|-----|
| Load test overview | `load-test-overview` |
| Umbraco CMS — RED | `umbraco-cms-red` |
| Umbraco CMS — .NET Runtime | `umbraco-cms-runtime` |
| Umbraco Web — RED | `umbraco-web-red` |
| Pod resources (Step 2) | `k8s-pod-resources` |
| Cluster (Step 3) | `k8s-cluster-overview` |

## Part 1 — Baseline under load (5 min)

1. Open **Load test overview** (`/d/load-test-overview`)
2. Run: `powershell -File ./scripts/k6-cms-load.ps1 -UsePodman`
3. Show: CMS RPS ↑, p95 stable, errors ~0
4. Drill into **Umbraco CMS — RED** and **CMS Runtime**

## Part 2 — Traces and logs (5 min)

1. Explore → Tempo: `{ resource.service.name = "umbraco-cms" }`
2. Loki: `{namespace="demo-services"}` → line with `trace_id` / `TraceId` → **View trace**
3. Search span: `ExamineSearch` on CMS

## Part 3 — RUM (optional, 3 min)

1. Browse `https://demo-umbr-fe.1node.tech`
2. Faro sessions in Loki (`sdk_name=faro-web`) / Explore

## Part 4 — Profiles (3 min)

1. Pyroscope: `umbraco-cms` under k6
2. Flame graph hot path

## Part 5 — After feature release (catch on thresholds)

1. Deploy CMS; flags `Demo__NaiveArticlesEndpoint` + `Demo__SlowEndpoint` = true
2. Run same `cms-load` (mix includes new routes with **healthy** SLOs)
3. If k6 fails / p95 up vs Part 1 → CMS RED by route → Tempo / Pyroscope (localize N+1 vs delay)
4. Show Delivery still OK while `articles_naive` (or `demo_slow`) breaches SLO

## Part 6 — (optional) single-trace drill

curl `/umbraco/api/demo/articles-naive?take=10` or `/slow?delayMs=50` for one clean Tempo view

## Part 7 — Pod / cluster (after Step 2–3)

1. **Pod resources**: CMS CPU/memory during cms-load
2. **Cluster overview**: node pressure, pod phases

## Q&A

- Baseline numbers: [BASELINE.md](./BASELINE.md)
- Beyla: [BEYLA_VS_OTEL.md](./BEYLA_VS_OTEL.md) (Phase 8)
