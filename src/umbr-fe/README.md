# umbr-fe — Next.js frontend (Phase 2)

Public **Tech Blog Demo** frontend: App Router, shadcn/ui, server-side calls to Umbraco Delivery API and search.

## Prerequisites

- Umbraco CMS running (see [../umbraco/README.md](../umbraco/README.md)) — default `http://localhost:8080`
- Node.js 22+ (LTS)

## Setup

```bash
cd src/umbr-fe
cp .env.example .env.local
npm install
```

Set `CMS_BASE_URL` to your CMS origin (local Podman or `https://demo-cms.1node.tech`).

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
