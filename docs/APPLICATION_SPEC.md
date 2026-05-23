# Sample application specification

Functional requirements for Umbraco CMS and Next.js web — sufficient for **realistic k6 load** and monitoring demo (latency, errors, DB, cache, search).

---

## Domain: “Tech Blog Demo”

Content site: articles, categories, authors, search, static About page.

---

## Umbraco CMS (`src/umbraco/`)

### Content types (Document Types)

| Alias | Fields | Purpose |
|-------|--------|---------|
| `article` | title (text), slug (text), excerpt (text), body (richtext), publishDate (date), featuredImage (media), category (MNTP → category), author (MNTP → author) | Main content |
| `category` | name, slug, description | List filtering |
| `author` | name, slug, bio, avatar (media) | Article detail |
| `siteSettings` | siteName, tagline, aboutBody (richtext), contactEmail | Singleton for `/about` |

**Seed volumes (minimum):**

- 100+ `article` (evenly across 5 categories)
- 5 `category`
- 10 `author`
- 1 `siteSettings`
- 20+ media items (featured + avatars)

### Public API

#### Delivery API (headless)

- `GET /umbraco/delivery/api/v2/content` — article list with pagination (`skip`, `take`)
- `GET /umbraco/delivery/api/v2/content/item/{id|path}` — detail by slug/path
- Filter by category: query or dedicated endpoint (implementation choice; document in `docs/API_CMS.md`)

#### Custom API (load and demo)

| Method | Path | Behavior | k6 |
|--------|------|----------|-----|
| GET | `/umbraco/api/search?q=&page=&pageSize=` | Full-text Examine search | ✅ high |
| GET | `/umbraco/api/demo/slow?delayMs=50-500` | Artificial delay (feature flag `Demo:SlowEndpoint`) | ✅ stress |
| GET | `/umbraco/api/demo/fault?rate=0.1` | 10% responses 500 (feature flag) | ✅ demo incident |
| GET | `/health` | Always 200 | ✅ probe |
| GET | `/ready` | 200 if DB OK, else 503 | ✅ probe |

**Management API:** seed/admin only; do not use from public Next.js.

### Non-functional

- Structured logs: `level`, `message`, `requestId` (Phase 1); + `trace_id` (Phase 5)
- CORS on CMS: **not required** when Next.js uses server-side BFF only (no browser → CMS cross-origin)
- Response compression: enabled (Brotli/Gzip middleware)

---

## Next.js Web (`src/umbr-fe/`)

### Pages (App Router)

| Route | Data source | Cache | k6 weight |
|-------|-------------|-------|-----------|
| `/` | Latest 6 articles + siteSettings | revalidate 60 | high |
| `/articles` | Paginated list, optional `?category=` | revalidate 60 | high |
| `/articles/[slug]` | Article by slug | revalidate 300 | high |
| `/search` | Query param `q` | no cache / short | medium |
| `/about` | siteSettings | revalidate 3600 | low |

### Route Handlers (BFF)

| Route | Upstream | Purpose |
|-------|----------|---------|
| `GET /api/articles?skip=&take=&category=` | Delivery API | JSON list for client/SSR |
| `GET /api/articles/[slug]` | Delivery API | Detail |
| `GET /api/search?q=` | CMS `/umbraco/api/search` | Search |
| `GET /api/health` | — | K8s probe |

### UI (shadcn)

- **Layout:** header (nav), footer
- **Components:** Card (article preview), Input (search), Button, Skeleton (loading), Pagination
- **States:** loading skeleton, empty search, error toast

### Non-functional

- `next/image` for media from CMS domain
- Error boundary on `[slug]`
- Env validation at build (zod or manual check)

---

## User stories

1. **Reader** opens home and navigates to an article
2. **Reader** browses catalog with pagination
3. **Reader** filters by category
4. **Reader** searches by keyword
5. **k6** generates mixed load by weights
6. **Incident demo** (Phase 7): enable fault endpoint + show alert/trace

---

## Feature flags (config)

| Flag | Default | Phase |
|------|---------|-------|
| `Demo__SlowEndpoint` | false | 1 (code), 7 (demo on) |
| `Demo__FaultEndpoint` | false | 1 (code), 7 (demo on) |

---

## Monitoring contracts (preparation)

Even in Phases 1–2, use **stable route names** for future metrics:

- `http.route`: `/articles`, `/api/search`, `/umbraco/api/search`
- Do not put random query values in route template metric labels (normalize in OTel Phase 5)

---

## Out of MVP scope

- User registration / auth
- Comments
- GraphQL
- Umbraco Heartcore cloud
- Content editing from frontend

---

## Acceptance (applications — Phase 3 K8s complete)

### Umbraco (Phase 1) — complete

- [x] Seed on first start (`DemoContentSeedHostedService`; idempotent if content exists)
- [x] Search returns results for seed terms (`GET /umbraco/api/search`)
- [x] Slow/fault endpoints off by default (`Demo__SlowEndpoint` / `Demo__FaultEndpoint` false in Production/K8s)
- [x] Delivery API + health/ready + Containerfile + `docs/API_CMS.md`

### Web (Phase 2) — complete

- [x] All web pages show real content (`src/umbr-fe/`, BFF route handlers, `src/umbr-fe/README.md`)
- [x] `Dockerfile` + `npm run build`; lockfile includes `@emnapi/*` for Linux `npm ci`
- [x] CI/CD: `.github/workflows/umbr-fe-container.yml` → GHCR → `deployment/k8s/apps/umbr-fe/`
- [x] Prod: `https://demo-umbr-fe.1node.tech` (SSR, search, CMS images)
- [x] Loading skeletons (`loading.tsx`), search `error.tsx` + toast, explicit `sharp` for Docker/Alpine images

### K8s apps (Phase 3) — complete

- [x] Namespace `demo-services` — `deployment/k8s/apps/namespace.yaml`; CI workflows deploy with `-n demo-services`
- [x] Umbraco + web manifests, IngressRoute, HPA (web); runbook [RUNBOOK_LOCAL_K8S.md](./RUNBOOK_LOCAL_K8S.md)
- [x] Prod Ingress: [demo-cms.1node.tech](https://demo-cms.1node.tech), [demo-umbr-fe.1node.tech](https://demo-umbr-fe.1node.tech)

### Observability stack (Phase 4) — complete

- [x] Namespace `observability` — LGTM+ (Mimir, Loki, Tempo, Pyroscope, Grafana, Alloy gateway + DaemonSet, MinIO, OpenMaxIO)
- [x] Grafana UI and datasources; Pyroscope UI; alloy-gateway OTLP (placeholder config — signals to backends in Phase 5)
- [x] Runbook [RUNBOOK_OBSERVABILITY.md](./RUNBOOK_OBSERVABILITY.md); manifests `deployment/k8s/observability/`
