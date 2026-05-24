# umbr-fe — Next.js frontend (Phase 2)

Public **Tech Blog Demo** frontend: App Router, shadcn/ui, server-side calls to Umbraco Delivery API and search.

## Prerequisites

- Umbraco CMS running (see [../umbraco/README.md](../umbraco/README.md)) — default `http://localhost:8080`
- Node.js 22+ (LTS)

## Setup

```bash
cd src/umbr-fe
cp .env.example .env.local
# Edit CMS_BASE_URL if CMS is not on http://localhost:8080
npm install
```

Environment: [`.env.example`](./.env.example) → `.env.local`. K8s: `deployment/k8s/apps/umbr-fe/configmap.yaml`.

## Run

```bash
npm run dev
```

Open [http://localhost:3000](http://localhost:3000).

## Routes

| Route | Cache |
|-------|-------|
| `/` | 60s |
| `/articles` | 60s |
| `/articles/[slug]` | 300s |
| `/search?q=` | no cache |
| `/about` | 3600s |
| `GET /api/health` | — |
| `GET /api/articles` | BFF JSON |
| `GET /api/search?q=` | BFF JSON |

Article URLs use the **content slug** (`article-001`, …) from seed; the CMS client maps to Delivery API item paths when needed.

## Docker / Kubernetes

```bash
docker build -f Dockerfile --build-arg CMS_BASE_URL=https://demo-cms.1node.tech -t monitoring-demo-umbr-fe:local .
docker run --rm -p 3000:3000 \
  -e CMS_BASE_URL=https://demo-cms.1node.tech \
  -e NEXT_PUBLIC_SITE_URL=http://localhost:3000 \
  monitoring-demo-umbr-fe:local
```

CI/CD and cluster manifests: [deployment/k8s/apps/umbr-fe/README.md](../../deployment/k8s/apps/umbr-fe/README.md), workflow [`.github/workflows/umbr-fe-container.yml`](../../.github/workflows/umbr-fe-container.yml).

`CMS_BASE_URL` is required at **build** time (image remote patterns) and **runtime** (fetch).

## Build

```bash
CMS_BASE_URL=http://localhost:8080 npm run build
```

## Observability (Phase 5 ✅)

- Node OTel: `src/instrumentation.ts` → `instrumentation.node.ts`
- Faro RUM: `FaroProvider` + `/api/faro` BFF
- **Distributed trace:** Faro adds `traceparent` on same-origin fetch; `cmsFetch` injects it to Umbraco; CMS continues to SQL

After deploy, in Grafana Tempo open a trace from `{ resource.service.name = "umbraco-web" && name = "cms.fetch" }` — one trace ID should include `umbraco-web` and `umbraco-cms` (+ SQL children). See [docs/RUNBOOK_OBSERVABILITY.md](../../docs/RUNBOOK_OBSERVABILITY.md).
