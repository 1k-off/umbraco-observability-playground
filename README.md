# Umbraco + React Monitoring Demo

Demo project: headless **Umbraco** (CMS/API) + **Next.js** (frontend with shadcn/ui), deployed on **Kubernetes**, with a full observability stack (Grafana, Mimir, Loki, Tempo, Pyroscope, Grafana Alloy, Grafana Faro, optionally Beyla).

## Goal

Demonstrate end-to-end monitoring of a real application: metrics, logs, traces, profiling, RUM — and prove with **k6** load that signals correlate with system behavior.

## Documentation

| Document | Purpose |
|----------|---------|
| [AGENTS.md](./AGENTS.md) | Primary instructions for AI agents |
| [docs/IMPLEMENTATION_PLAN.md](./docs/IMPLEMENTATION_PLAN.md) | Phased implementation plan |
| [docs/ARCHITECTURE.md](./docs/ARCHITECTURE.md) | Target architecture |
| [docs/MONITORING_STACK.md](./docs/MONITORING_STACK.md) | Observability stack and signals |
| [docs/K6_LOAD_TESTING.md](./docs/K6_LOAD_TESTING.md) | Load scenarios and SLOs |
| [docs/APPLICATION_SPEC.md](./docs/APPLICATION_SPEC.md) | Sample application functionality |
| [deployment/k8s/sqlserver/README.md](./deployment/k8s/sqlserver/README.md) | SQL Server on remote K8s |

## Phases (summary)

1. **Sample apps** — full Umbraco + Next.js (no observability in application code).
2. **k6** — load scenarios and baseline thresholds.
3. **Kubernetes** — applications and dependencies.
4. **Observability** — Alloy, Mimir, Loki, Tempo, Pyroscope, Grafana, Faro (+ Beyla).
5. **Instrumentation** — OTel, logs, RUM, eBPF.
6. **Demo** — dashboards, alerts, runbook, reproducible walkthrough.

Details are in [docs/IMPLEMENTATION_PLAN.md](./docs/IMPLEMENTATION_PLAN.md).

## Agent skills

- **dotnet/skills** — 78 skills in `.agents/skills/` (ASP.NET, EF, OTel, diagnostics, MSBuild). Install: `npx skills add dotnet/skills --agent cursor -y --all`
- **Umbraco / Next / shadcn** — see `skills-lock.json` and `.agents/skills/`

## Status

- **SQL Server** — `deployment/k8s/sqlserver/`
- **Umbraco 17.4.2** — `src/umbraco/` (Podman build/run)
- **Next.js** — Phase 2

