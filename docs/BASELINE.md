# Performance baseline (k6)

Filled in **Phase 3** after first runs **without** observability overhead.

## Environment

| Field | Value |
|-------|-------|
| Date | _TBD_ |
| Target | local / k8s |
| Web URL | _TBD_ |
| CMS URL | _TBD_ |
| Web / CMS replicas | _TBD_ |

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

## cms-direct.js

| Metric | Value |
|--------|-------|
| RPS | _TBD_ |
| p95 search | _TBD_ |
| p95 delivery list | _TBD_ |

## After Phase 6

Add a “with observability” section and compare overhead (%).
