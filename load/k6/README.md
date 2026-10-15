# k6 load tests (CMS-first)

Primary target: **Umbraco CMS** (`K6_CMS_BASE_URL`). Web scenarios are secondary.

Suite covers Delivery, search, health, plus post-release routes `articles-naive` and `demo/slow` (CMS flags required). **Thresholds are healthy SLOs** — tests do not assume a bug; a fail after release is what triggers Grafana investigation.

## Prerequisites

- [k6](https://grafana.com/docs/k6/latest/set-up/install-k6/) on PATH, **or** Podman/Docker (`grafana/k6` image)
- CMS reachable (default `https://demo-cms.1node.tech`)
- `Demo__NaiveArticlesEndpoint` / `Demo__SlowEndpoint` = true when those routes are in the mix

## Scenarios

| Script | Role |
|--------|------|
| `cms-smoke.js` | Quick CMS regression (~1 min) |
| `cms-load.js` | Main 10 min CMS mix |
| `cms-stress.js` | Ramp to find knee |
| `cms-spike.js` | Burst traffic |
| `cms-direct.js` | Fixed 20 VU / 10 min CMS isolation |
| `web-smoke.js` | Light frontend smoke |
| `web-load.js` | Short frontend load |

## Env

| Variable | Default | Meaning |
|----------|---------|---------|
| `K6_CMS_BASE_URL` | `https://demo-cms.1node.tech` | CMS base |
| `K6_WEB_BASE_URL` | `https://demo-umbr-fe.1node.tech` | Web base |

## Runners

```powershell
powershell -File ./scripts/k6-cms-smoke.ps1 -UsePodman
powershell -File ./scripts/k6-cms-load.ps1 -UsePodman
powershell -File ./scripts/k6-web-smoke.ps1 -UsePodman
```

## Fixtures

- `data/slugs.json` — seed article slugs `article-001` … `article-100`
- `data/search-terms.json` — search query pool

Regenerate slugs after changing seed count (keep in sync with `DemoContentSeedHostedService`).
