# k6 load testing

Scenarios, thresholds, and integration with the monitoring demo. Directory: `load/k6/`.

**Phase 6 focus: CMS-first.** Primary load hits Umbraco/.NET (`demo-cms`). Web scenarios are secondary (prove the FE stack; not the article narrative).

---

## Goals (Phase 6 — after instrumentation)

1. **SLO validation:** CMS smoke / load / stress / spike / cms-direct, plus light web smoke/load.
2. **Observability correlation:** under `cms-load.js`, prove RPS/p95/errors in Mimir, traces in Tempo, `trace_id` in Loki, profiles in Pyroscope.
3. **Baseline:** record RPS, p95, error rate in `docs/BASELINE.md` for the instrumented k8s environment.
4. **New CMS routes in the suite:** `articles-naive` and `demo/slow` (flags on CMS). Thresholds are **healthy SLOs** — tests do not assume a bug. If p95/checks fail after release → investigate in Grafana (Tempo/Pyroscope). k6 does **not** toggle flags.

---

## Environments

| Env | Web base URL | CMS base URL |
|-----|--------------|--------------|
| local (Podman/Docker CMS) | `http://localhost:3000` | `http://localhost:8080` |
| k8s (demo cluster) | `https://demo-umbr-fe.1node.tech` | `https://demo-cms.1node.tech` |

Pass via env: `K6_WEB_BASE_URL`, `K6_CMS_BASE_URL`.

---

## Scenarios (files)

| File | Type | Duration | VUs | Purpose |
|------|------|----------|-----|---------|
| `cms-smoke.js` | smoke | ~1 min | 3 | Primary CI / quick CMS check |
| `cms-load.js` | load | 10 min | ramp → 50 | Main CMS demo / article |
| `cms-stress.js` | stress | ~15 min | ramp → 150 | Knee / limits |
| `cms-spike.js` | spike | ~5 min | 5→100→5 | Burst |
| `cms-direct.js` | load | 10 min | 20 | CMS isolation (same mix) |
| `web-smoke.js` | smoke | ~1 min | 2 | Secondary FE check |
| `web-load.js` | load | ~4 min | ramp → 15 | Secondary FE load |

Shared: `lib/config.js`, `lib/checks.js`, `lib/cms.js`, `lib/web.js`, `data/slugs.json`, `data/search-terms.json`.

---

## Traffic mix (cms-load / cms-direct / stress / spike)

| Operation | Weight | Endpoint |
|-----------|--------|----------|
| Delivery list | ~30% | `GET /umbraco/delivery/api/v2/content?filter=contentType:article&…` |
| Delivery item | ~25% | `GET /umbraco/delivery/api/v2/content/item/{path}` |
| Search | ~15% | `GET /umbraco/api/search?q=` |
| Articles enrichment | ~20% | `GET /umbraco/api/demo/articles-naive?take=10` |
| Demo slow | ~5% | `GET /umbraco/api/demo/slow?delayMs=50` |
| Health | ~5% | `GET /health` |

Requires CMS flags `Demo__NaiveArticlesEndpoint` / `Demo__SlowEndpoint` = true (otherwise 404 → failed checks).

**Slug pool:** `load/k6/data/slugs.json` (`article-001`…`article-100`; Delivery path maps to `article-1`).

**Search terms:** `load/k6/data/search-terms.json`.

**Catching regressions:** SLOs for new routes match Delivery-class latency. A failed threshold after release is the signal to open Grafana — not something the test “expects”.

---

## Thresholds

### cms-smoke.js

```javascript
thresholds: {
  http_req_failed: ['rate<0.01'],
  http_req_duration: ['p(95)<2000'],
}
```

Smoke also hits `articles-naive` and `slow` once per iteration.

### cms-load.js / cms-direct.js

```javascript
thresholds: {
  http_req_failed: ['rate<0.02'],
  http_req_duration: ['p(95)<1500'],
  'http_req_duration{endpoint:delivery_list}': ['p(95)<1200'],
  'http_req_duration{endpoint:search}': ['p(95)<2000'],
  'http_req_duration{endpoint:articles_naive}': ['p(95)<1200'],
  'http_req_duration{endpoint:demo_slow}': ['p(95)<200'],
}
```

### cms-stress.js

- No strict p95 — find degradation point; document in BASELINE
- Soft `http_req_failed` &lt; 15%

---

## Running

```powershell
# CMS (primary)
powershell -File ./scripts/k6-cms-smoke.ps1 -UsePodman
powershell -File ./scripts/k6-cms-load.ps1 -UsePodman
powershell -File ./scripts/k6-cms-direct.ps1 -UsePodman
powershell -File ./scripts/k6-cms-stress.ps1 -UsePodman
powershell -File ./scripts/k6-cms-spike.ps1 -UsePodman

# Web (secondary)
powershell -File ./scripts/k6-web-smoke.ps1 -UsePodman
powershell -File ./scripts/k6-web-load.ps1 -UsePodman
```

`-UsePodman` runs `grafana/k6` when local `k6` is not installed.

---

## Article correlation checklist (A + D)

### Baseline (flags off)

1. Run `cms-load.js` (~10 min).
2. Grafana Explore → Mimir: `http_server_request_duration_seconds_*` for `service_name="umbraco-cms"` — RPS↑, stable p95.
3. Tempo: Delivery/search traces without N+1 fan-out.
4. Loki: CMS JSON logs with `trace_id`.
5. Pyroscope: `umbraco-cms` profiles under load.

### If thresholds fail after release (investigation)

1. Confirm flags/routes are the new feature under test (`articles_naive`, `demo_slow`).
2. k6 summary: which tag breached SLO (`articles_naive` vs Delivery still green → localize to new API).
3. Tempo: slow traces on that route — e.g. `DemoNaiveArticles` fan-out GetById (N+1) or `DemoSlowEndpoint` delay.
4. Mimir RED by route + Runtime + Pyroscope to confirm.

---

## CI integration

| Job | When | Scenario |
|-----|------|----------|
| PR | optional | `cms-smoke` |
| nightly / manual | after Phase 6 | `cms-load` + artifact summary |
| pre-demo | manual | cms-load + correlation checklist |

---

## Link to monitoring

| k6 observation | Grafana |
|----------------|---------|
| RPS increase | Mimir `rate(http_server_…)` on `umbraco-cms` |
| p95 latency | RED / Explore |
| N+1 (A) | Tempo fan-out on `DemoNaiveArticles` |
| Slow (D) | Tempo `DemoSlowEndpoint` |
| 5xx | Loki + Tempo error spans |
| Profiles | Pyroscope `umbraco-cms` |

---

## Phase 6 DoD

- [x] Scenario files in `load/k6/` (cms-smoke/load/stress/spike/direct + web-smoke/load)
- [x] `lib/` helpers; `data/slugs.json` and `search-terms.json`
- [x] **cms-smoke** exit 0 on k8s Ingress
- [x] **cms-load** 10 min meets thresholds ([BASELINE](./BASELINE.md))
- [x] **cms-stress** / **cms-spike** run documented in BASELINE
- [x] **cms-direct** exit 0 (isolated re-run)
- [x] **web-smoke** / **web-load** exit 0 (secondary)
- [x] `docs/BASELINE.md` filled
- [x] Correlation checklist documented ([run-docs/12](../run-docs/12-cms-load-correlation.md)); A/D live verification after CMS image deploy + flags
