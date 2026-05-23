# Umbraco + Next.js Monitoring Demo

Demo project: headless **Umbraco** (CMS/API) + **Next.js** (frontend with shadcn/ui), deployed on **Kubernetes**, with a full observability stack (Grafana, Mimir, Loki, Tempo, Pyroscope, Grafana Alloy, Grafana Faro, optionally Beyla).

## Goal

Demonstrate end-to-end monitoring of a real application: metrics, logs, traces, profiling, RUM — and prove with **k6** load that signals correlate with system behavior.

## Documentation

| Document | Purpose |
|----------|---------|
| [AGENTS.md](./AGENTS.md) | Primary instructions for AI agents |
| [docs/IMPLEMENTATION_PLAN.md](./docs/IMPLEMENTATION_PLAN.md) | Phased implementation plan (Phases 0–8) |
| [docs/ARCHITECTURE.md](./docs/ARCHITECTURE.md) | Target architecture |
| [docs/APPLICATION_SPEC.md](./docs/APPLICATION_SPEC.md) | Sample application functionality |
| [docs/API_CMS.md](./docs/API_CMS.md) | CMS HTTP examples |
| [docs/MONITORING_STACK.md](./docs/MONITORING_STACK.md) | Observability stack and signals |
| [docs/K6_LOAD_TESTING.md](./docs/K6_LOAD_TESTING.md) | Load scenarios and SLOs |
| [docs/BASELINE.md](./docs/BASELINE.md) | k6 baseline (Phase 6) |
| [docs/DEMO_SCRIPT.md](./docs/DEMO_SCRIPT.md) | Presentation script (Phase 7) |
| [deployment/k8s/sqlserver/README.md](./deployment/k8s/sqlserver/README.md) | SQL Server on remote K8s |

## Phases (summary)

Aligned with [docs/IMPLEMENTATION_PLAN.md](./docs/IMPLEMENTATION_PLAN.md):

| Phase | Focus |
|-------|--------|
| 0 | Docs and agent rules ✅ |
| 1 | Umbraco CMS ✅ |
| 2 | Next.js + shadcn ✅ |
| 3 | K8s apps + ingress ✅ |
| 4 | K8s observability stack ✅ |
| 5 | OTel, Alloy, Faro, profiles ← current |
| 6 | k6 full load testing |
| 7 | Dashboards, alerts, demo script |
| 8 | Beyla (optional) |

## Agent skills

- **dotnet/skills** — 78 skills in `.agents/skills/` (ASP.NET, EF, OTel, diagnostics, MSBuild). Install: `npx skills add dotnet/skills --agent cursor -y --all`
- **Umbraco / Next / shadcn** — see `skills-lock.json` and `.agents/skills/`
- Project skill: [.cursor/skills/umbraco-monitoring-demo/SKILL.md](./.cursor/skills/umbraco-monitoring-demo/SKILL.md)

## Status

| Component | Path | Phase |
|-----------|------|-------|
| SQL Server manifests | `deployment/k8s/sqlserver/` | Applied (remote cluster) |
| Umbraco 17.4.2 | `src/umbraco/` | Phase 1 ✅ ([prod CMS](https://demo-cms.1node.tech)) |
| Next.js | `src/umbr-fe/` | Phase 2 ✅ ([prod web](https://demo-umbr-fe.1node.tech)) |
| App K8s | `deployment/k8s/apps/` | Phase 3 ✅ (`demo-services`) |
| Observability K8s | `deployment/k8s/observability/` | Phase 4 ✅ (cluster verified) |
| k6 | `load/k6/` | Phase 6 (after instrumentation) |

Local CMS: **Podman** on `http://localhost:8080` — see [src/umbraco/README.md](./src/umbraco/README.md).  
Web local: `cd src/umbr-fe && npm run dev` — prod [demo-umbr-fe.1node.tech](https://demo-umbr-fe.1node.tech).
