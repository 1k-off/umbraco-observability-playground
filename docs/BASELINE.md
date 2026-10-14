# Performance baseline (k6)

Filled in **Phase 6** after CMS-first k6 runs against the **instrumented** k8s stack (Phases 3–5 complete).

## Environment

| Field | Value |
|-------|-------|
| Date | 2026-10-12 |
| Target | k8s (primary) |
| Web URL | `https://demo-umbr-fe.1node.tech` |
| CMS URL | `https://demo-cms.1node.tech` |
| Web / CMS replicas | web HPA 2–5; CMS 1 (as deployed) |
| Observability | Mimir, Loki, Tempo, Pyroscope, Alloy, Faro (Phase 5 ✅) |
| Runner | Podman `grafana/k6:latest` |
| Demo flags | Naive/Slow/Fault **off** (healthy baseline) |

## cms-smoke.js (~1 min, 3 VU)

| Metric | Value |
|--------|-------|
| http_req_duration p95 | 118.54 ms |
| http_req_failed rate | 0.00% |
| http_reqs | 528 (~8.7/s) |
| checks | 100% (528/528) |
| Result | **pass** |

## cms-load.js (10 min, ramp → 50 VU)

| Metric | Value |
|--------|-------|
| RPS (http_reqs) | ~53.7/s (32212 reqs / 10m) |
| p95 | 113.75 ms |
| p95 delivery_list | 121.9 ms |
| p95 search | 110.35 ms |
| max | 571.73 ms |
| error rate | 0.01% (6/32212) |
| Result | **pass** (all thresholds) |

## cms-stress.js (~15 min, ramp → 150 VU)

| Metric | Value |
|--------|-------|
| Max VUs reached | 150 |
| http_req_failed | 0.00% |
| Overall p95 | 1.53 s |
| Overall p90 | 1.24 s |
| avg duration | 502.5 ms |
| RPS | ~86/s (77400 reqs) |
| Knee point (notes) | No hard failure (0% errors). Latency knee: p95 climbs above ~1s under high VU (≈80–150). Soft limit is **latency**, not errors, on single CMS replica. |
| Result | **completed** (soft threshold `rate<0.15` pass) |

## cms-spike.js (~5 min, 5→100→5 VU)

| Metric | Value |
|--------|-------|
| Peak VUs | 100 |
| Peak error rate | 0.00% |
| p95 (overall) | 208.66 ms |
| RPS | ~161/s (48427 reqs) |
| Recovery time (notes) | Ramp-down to 5 VU completed cleanly; no elevated errors during or after spike. |
| Result | **pass** |

## cms-direct.js (5 min isolated, 20 VU; `--duration=5m`)

| Metric | Value |
|--------|-------|
| RPS | ~33.3/s (9996 reqs) |
| p95 | 137.21 ms |
| p95 search | 140.93 ms |
| p95 delivery list | 142.48 ms |
| error rate | 0.00% |
| Result | **pass** (re-run alone after stress; concurrent-with-stress run had elevated list p95) |

## web-smoke.js (~1 min, 2 VU) — secondary

| Metric | Value |
|--------|-------|
| http_req_duration p95 | 274.61 ms |
| http_req_failed rate | 0.00% |
| Result | **pass** |

## web-load.js (~4 min, ramp → 15 VU) — secondary

| Metric | Value |
|--------|-------|
| RPS | ~10.2/s (2458 reqs) |
| p95 | 246.51 ms |
| p95 home | 125.87 ms |
| p95 search | 219.54 ms |
| error rate | 0.00% |
| Result | **pass** |

## Observability correlation (Phase 6 checklist)

| Signal | Visible during `cms-load.js`? | Notes |
|--------|-------------------------------|-------|
| Mimir RED | Verify in Grafana Explore | Load ~54 RPS; stress/spike also drove traffic to `umbraco-cms` |
| Tempo traces | Verify in Grafana | Delivery + ExamineSearch; no N+1 until naive flag deploy |
| Loki `trace_id` | Verify in Grafana | CMS JSON logs (Phase 5) |
| Pyroscope | Verify in Grafana | `umbraco-cms` under load/stress |
| Faro (optional) | N/A for CMS-first | Secondary web only |

Procedure: [run-docs/12-cms-load-correlation.md](../run-docs/12-cms-load-correlation.md).

### Antipattern A (N+1) — after deploy + `Demo__NaiveArticlesEndpoint=true`

| Signal | Expected |
|--------|----------|
| Tempo | `DemoNaiveArticles` → many `ResolveArticleRelations` / `GetById` |
| Mimir | Higher latency on naive path vs Delivery list |
| Probe | curl / PowerShell against `/umbraco/api/demo/articles-naive` (not part of k6 mix) |

**Status:** Code + flag shipped in repo; **not yet on cluster image** at baseline date (endpoint 404 until deploy).

### Antipattern D (slow) — `Demo__SlowEndpoint=true`

| Signal | Expected |
|--------|----------|
| Tempo | `DemoSlowEndpoint` + `demo.delay_ms` (no SQL fan-out) |
| Probe | curl against `/umbraco/api/demo/slow` (not part of k6 mix) |

**Status:** Endpoint already in prod image; enable flag for demo run.
