# Demo script (presentation)

Completed in **Phase 7**. Draft structure — guide for the agent.

## Preparation (5 min)

- [x] K8s apps (Phase 3): namespace `demo-services`, CMS + web Ingress — already deployed
- [x] K8s observability (Phase 4 ✅): namespace `observability`, Grafana Ingress + datasources
- [x] Grafana login (ingress or port-forward)
- [ ] Dashboards imported
- [ ] `Demo__SlowEndpoint` and `Demo__FaultEndpoint` = false

## Part 1 — Baseline under load (5 min)

1. Open **Overview** dashboard
2. Run `k6 run load/k6/load.js`
3. Show: RPS ↑, stable latency, error rate ~0

## Part 2 — Traces and logs (5 min)

1. Grafana Explore → Tempo: trace `umbraco-web` → CMS fetch span
2. Loki: log line with `trace_id` → **View trace**
3. Show search request: dedicated span on CMS

## Part 3 — RUM (3 min, Phase 5 ✅)

1. Open site in browser (or k6 browser)
2. **Faro** dashboard: Web Vitals, session
3. Link to backend trace (traceparent)

## Part 4 — Profiles (3 min)

1. Pyroscope: `umbraco-cms` under k6
2. Flame graph hot path (search / SQL)

## Part 5 — Incident (5 min)

1. Enable `Demo__FaultEndpoint=true`
2. k6 load 3 min → **HighErrorRate** alert
3. Tempo: error spans; Loki: exceptions
4. Disable fault; recovery

## Part 6 — Slow path (optional)

1. `Demo__SlowEndpoint=true`, delay 300ms
2. p95 alert / dashboard panel

## Q&A

- Observability overhead: see [BASELINE.md](./BASELINE.md)
- Beyla: [BEYLA_VS_OTEL.md](./BEYLA_VS_OTEL.md) (Phase 8)
