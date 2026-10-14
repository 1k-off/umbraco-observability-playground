# AGENTS.md — Umbraco Monitoring Demo

Instructions for AI agents (Cursor, etc.) working in this repository.

## Project mission

Build a **full monitoring demo** for **headless Umbraco + Next.js (shadcn)** on **Kubernetes**. Sample applications must be **production-like** (real functionality, loadable with **k6**). Deploy apps and instrumentation first; **full k6 load testing** (Phase 6) runs against the instrumented stack.

## Required reading before work

Before any implementation, read the relevant files:

| Task | Document |
|------|----------|
| Phase order, completion criteria | [docs/IMPLEMENTATION_PLAN.md](./docs/IMPLEMENTATION_PLAN.md) |
| CMS and frontend functionality | [docs/APPLICATION_SPEC.md](./docs/APPLICATION_SPEC.md) |
| Architecture and service boundaries | [docs/ARCHITECTURE.md](./docs/ARCHITECTURE.md) |
| Mimir/Loki/Tempo/Alloy/Faro/Beyla | [docs/MONITORING_STACK.md](./docs/MONITORING_STACK.md) |
| k6 scenarios and SLOs | [docs/K6_LOAD_TESTING.md](./docs/K6_LOAD_TESTING.md) |
| CMS HTTP examples | [docs/API_CMS.md](./docs/API_CMS.md) |
| k6 baseline numbers | [docs/BASELINE.md](./docs/BASELINE.md) |
| Demo walkthrough | [docs/DEMO_SCRIPT.md](./docs/DEMO_SCRIPT.md) |
| K8s apps deploy | [docs/RUNBOOK_LOCAL_K8S.md](./docs/RUNBOOK_LOCAL_K8S.md) (Phase 3 ✅) |
| Observability deploy | [docs/RUNBOOK_OBSERVABILITY.md](./docs/RUNBOOK_OBSERVABILITY.md) (Phase 4 ✅) |

## Hard rules

1. **Do not skip ahead.** Monitoring code (OTel SDK, Faro, Alloy config, Helm observability) only when [IMPLEMENTATION_PLAN](./docs/IMPLEMENTATION_PLAN.md) explicitly allows it for the current phase.
2. **Applications first, signals second.** Phases 1–5 (apps, K8s, obs stack, instrumentation) before **Phase 6** full k6 load testing.
3. **Minimal diff.** One phase / one logical PR chunk — one area of change. Do not refactor unrelated code.
4. **k6 is mandatory.** Any new load-bearing endpoint or user flow — update [docs/K6_LOAD_TESTING.md](./docs/K6_LOAD_TESTING.md) and add/extend a scenario.
5. **No secrets in git.** Connection strings, API keys, Grafana admin — K8s Secret templates, `appsettings.Development.json.example` (CMS), `src/umbr-fe/.env.example` (web), documentation.
6. **Linux-first for CI/K8s.** Windows locally is fine; pipelines and manifests target Linux containers.
7. **Commits** — only when the user explicitly asks.
8. After changing **global** conventions or `.cursor/skills/` — update this file (see `agents-md-sync` skill).

## Target repository structure (as implemented)

```
/
├── AGENTS.md
├── README.md
├── docs/                    # Plans and specs (source of truth for agents)
├── src/
│   ├── umbraco/             # Umbraco headless (.NET)
│   └── umbr-fe/             # Next.js + shadcn
├── deployment/
│   └── k8s/
│       ├── sqlserver/       # SQL Server StatefulSet (remote cluster)
│       ├── apps/            # Umbraco, web, ingress (Phase 3 ✅)
│       └── observability/   # Grafana stack (Phase 4 ✅)
├── docker-compose.yml       # Local apps; CMS uses remote SQL (Phase 1+)
├── load/
│   └── k6/                  # Scenarios, thresholds, helpers
├── observability/
│   ├── alloy/               # River config
│   ├── grafana/             # Dashboards, datasources as code
│   └── faro/                # Collector / gateway config
└── .cursor/
    ├── rules/
    └── skills/
```

Directories `src/`, `deployment/`, `load/` may not exist yet — create them according to the active plan phase.

## Local development model

- **No local .NET SDK required** — build and run Umbraco via **Docker** only.
- **Kubernetes is remote** — apply manifests with `kubectl` to the remote cluster.
- **SQL Server runs in remote K8s** — namespace `sqlserver`, LB IP `10.255.0.101:1433` (cluster MetalLB not in repo); see `deployment/k8s/sqlserver/README.md`.
- **Local containers** (Podman/Docker; Compose when added) run app services; Umbraco uses `appsettings.Development.json` (see `src/umbraco/README.md`); web uses `src/umbr-fe/.env.local`.

## Technology stack

| Layer | Choice | Notes |
|-------|--------|-------|
| CMS | Umbraco (headless), Delivery API | Content, media, search |
| API host | ASP.NET Core | Standard Umbraco host |
| Frontend | Next.js (App Router), TypeScript | SSR/ISR where appropriate |
| UI | shadcn/ui + Tailwind | Shared design system |
| DB | SQL Server (remote K8s; see `deployment/k8s/sqlserver/`) | Umbraco persistence |
| Orchestration | Kubernetes | Ingress, HPA as needed |
| Metrics | Prometheus → **Mimir** | Via Alloy scrape |
| Logs | **Loki** | Structured logs, trace_id in logs |
| Traces | **Tempo** | OTLP from apps + Alloy |
| Profiles | **Pyroscope** | .NET + Node continuous profiling |
| Collection | **Grafana Alloy** | Single agent: scrape, OTLP, logs |
| RUM | **Grafana Faro** | Next.js browser SDK |
| eBPF (opt.) | **Beyla** | HTTP/gRPC without code changes |
| Viz | **Grafana** | Dashboards, Explore, correlation |
| Load | **k6** | Scenarios in `load/k6/` |

## Phases and agent focus

See [docs/IMPLEMENTATION_PLAN.md](./docs/IMPLEMENTATION_PLAN.md). Summary:

| Phase | Agent focus | Observability |
|-------|-------------|---------------|
| 0 | Documentation, repo skeleton | None |
| 1 | Umbraco: content model, API, health ✅ | `/health`, `/ready` only |
| 2 | Next.js: pages, shadcn, CMS calls ✅ | Health only |
| 3 | K8s: apps, DB, ingress ✅ | None |
| 4 | K8s: observability namespace ✅ | Stack Helm/YAML |
| 5 | Alloy, exporters, OTel, Faro | Yes ✅ |
| 6 | k6: CMS-first suite (`cms-*` + light `web-*`); antipatterns A/D | Uses full stack |
| 7 | Dashboards, recording rules, demo runbook | Yes |
| 8 | Beyla (opt.), hardening | Yes |

**Default phase for agents: 7** (deploy/verify dashboards + optional alerts) unless the user names another phase. App/pod/cluster dashboard JSON and Alloy scrapes are in repo — apply via `scripts/apply-grafana-dashboards.ps1` and `scripts/install-cluster-metrics.ps1`. A/D demos: deploy `articles-naive` then enable CMS flags ([run-docs/12-cms-load-correlation.md](./run-docs/12-cms-load-correlation.md)).

## Code conventions (when implementation starts)

### Umbraco (`src/umbraco/`)

- Headless: Delivery API + Management API for seed/admin only, not for the public frontend.
- Pin .NET and Umbraco versions in `global.json` / README when creating the solution.
- Required endpoints: `GET /health`, `GET /ready` (ready checks DB).
- Logging: structured (JSON to stdout) with `trace_id`, `span_id` (Phase 5 ✅).
- Seed: script or one-shot startup job with demo content (articles, categories, media) for k6.

### Next.js (`src/umbr-fe/`)

- App Router, `src/` layout, `@/` aliases.
- shadcn: components in `components/ui/`, do not duplicate styles.
- CMS calls via server components / route handlers with caching (`revalidate`) as in APPLICATION_SPEC.
- Faro SDK (Phase 5 ✅).
- Env: `CMS_BASE_URL`, separate public vs server-only vars.

### Kubernetes (`deployment/k8s/`)

- Namespaces: `demo-services` (apps), `observability` (stack).
- Labels: `app.kubernetes.io/name`, `app.kubernetes.io/component`.
- Probes: liveness → `/health`, readiness → `/ready`.
- Set resource requests/limits before load tests (tune after Phase 6 k6 stress).

### Observability (`observability/`, `deployment/k8s/observability/`)

- Single ingress (or port-forward doc) for Grafana; everything else internal.
- Alloy — single entry point: Prometheus scrape, OTLP gateway, log tail/filelog.
- Align service names and labels with [MONITORING_STACK.md](./docs/MONITORING_STACK.md) (`service.name`, `deployment.environment`).

## Definition of Done (general)

- [x] Phase 1 criteria in IMPLEMENTATION_PLAN are met (Umbraco; verified prod health/Delivery API)
- [x] Phase 2 criteria in IMPLEMENTATION_PLAN (Next.js web)
- [x] Phase 3 criteria in IMPLEMENTATION_PLAN (K8s apps; prod Ingress verified)
- [x] Phase 4 criteria in IMPLEMENTATION_PLAN (LGTM+ in `observability`; Grafana datasources, Alloy, Pyroscope verified)
- [x] Phase 5 criteria in IMPLEMENTATION_PLAN (instrumentation; cluster DoD per run-docs/10)
- [x] Phase criteria for Phase 6 (CMS-first k6 scripts + BASELINE) are met; A/D live Grafana after CMS deploy
- [x] README / docs updated for Phase 1–6
- [x] Full k6 suite scripts pass on k8s Ingress (Phase 6)
- [ ] No secrets in git
- [ ] AGENTS.md updated for new cross-cutting rules

## Cursor skills (project)

| Skill | When to use |
|-------|-------------|
| [.cursor/skills/umbraco-monitoring-demo/SKILL.md](./.cursor/skills/umbraco-monitoring-demo/SKILL.md) | Any task in this repo |
| `gh-actions-dotnet-build` | CI for Umbraco |
| `gh-actions-nodejs-build` | CI for Next.js |
| `ci-cd-pipeline-foundation` | Pipeline structure |
| `shadcn` (plugin) | UI components |
| **dotnet/skills** (`.agents/skills/`) | .NET / ASP.NET / EF / OTel / diagnostics — see table below |

### dotnet/skills (installed in `.agents/skills/`)

Marketplace: [dotnet/skills](https://github.com/dotnet/skills). Installed via `npx skills add dotnet/skills --agent cursor -y --all`.

| Skill | Use in this repo |
|-------|------------------|
| `dotnet-webapi` | Umbraco CMS API, middleware, endpoints |
| `configuring-opentelemetry-dotnet` | Phase 5 instrumentation |
| `optimizing-ef-core-queries` | Umbraco / SQL performance |
| `analyzing-dotnet-performance` | Load / k6 follow-up |
| `dotnet-trace-collect`, `dump-collect` | Demo diagnostics |
| `run-tests`, `dotnet-test-frameworks` | CMS tests in Docker |
| `binlog-failure-analysis`, `build-perf-baseline` | Docker `dotnet build` |
| `csharp-scripts` | Quick .NET experiments without local SDK |

Reinstall or update: `npx skills update -p -y` (project scope).

## Do not

- Deploy the full Grafana stack before applications are ready in K8s (Phase 3 done).
- Add “hello world” CMS/frontend — implement APPLICATION_SPEC functionality.
- Mix Phase 1 and Phase 5 in one PR without explicit user request.
- Implement only k6 smoke in Phase 6 — the phase requires the **full** scenario set (see [K6_LOAD_TESTING.md](./docs/K6_LOAD_TESTING.md)).
- Switch to an alternative stack (Datadog, Elastic-only) — demo goal is **Grafana LGTM+**.

## Questions for the user (if blocked)

Ask only when genuinely uncertain:

- PostgreSQL instead of SQL Server (non-default)
- Managed K8s vs local (kind/k3d/minikube)
- Beyla in MVP vs Phase 8 only
- Domain/ingress (real DNS vs localhost)

## Updating this file

Add a section or bullet when a **new global rule** appears (folder structure, naming, phase, required tool). Implementation details belong in `docs/` — keep AGENTS.md concise.
