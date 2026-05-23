# Performance baseline (k6)

Filled in **Phase 6** after the **full** k6 suite runs against the **instrumented** k8s stack (Phases 3–5 complete).

## Environment

| Field | Value |
|-------|-------|
| Date | _TBD_ |
| Target | k8s (primary) / local (optional) |
| Web URL | _TBD_ |
| CMS URL | _TBD_ |
| Web / CMS replicas | _TBD_ |
| Observability | Mimir, Loki, Tempo, Pyroscope, Alloy, Faro (Phase 5) |

## smoke.js

| Metric | Value |
|--------|-------|
| http_req_duration p95 | _TBD_ |
| http_req_failed rate | _TBD_ |

## load.js (10 min, 50 VU)

| Metric | Value |
|--------|-------|
| RPS (http_reqs) | _TBD_ |
| p95 | _TBD_ |
| p99 | _TBD_ |
| error rate | _TBD_ |

## stress.js

| Metric | Value |
|--------|-------|
| Max stable VUs | _TBD_ |
| Knee point (notes) | _TBD_ |
| http_req_failed at knee | _TBD_ |

## spike.js

| Metric | Value |
|--------|-------|
| Recovery time (notes) | _TBD_ |
| Peak error rate | _TBD_ |

## cms-direct.js

| Metric | Value |
|--------|-------|
| RPS | _TBD_ |
| p95 search | _TBD_ |
| p95 delivery list | _TBD_ |

## Observability correlation (Phase 6 checklist)

| Signal | Visible during `load.js`? | Notes |
|--------|---------------------------|-------|
| Mimir RED | _TBD_ | |
| Tempo traces | _TBD_ | web → cms → sql |
| Loki `trace_id` | _TBD_ | |
| Pyroscope | _TBD_ | |
| Faro (optional) | _TBD_ | browser scenario |
