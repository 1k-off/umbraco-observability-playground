---
name: umbraco-monitoring-demo
description: >-
  Project skill for the Umbraco + Next.js Kubernetes monitoring demo.
  Use for any implementation task in this repository: CMS, web, k6, K8s,
  Grafana LGTM+ stack, Alloy, Faro, Beyla. Enforces phased delivery from
  docs/IMPLEMENTATION_PLAN.md.
---

# Umbraco Monitoring Demo

## Before you act

1. Read [AGENTS.md](../../../AGENTS.md)
2. Confirm active **phase** with the user if unclear
3. Open the matching section in [docs/IMPLEMENTATION_PLAN.md](../../../docs/IMPLEMENTATION_PLAN.md)

## Phase guardrails

| Phase | You may implement | You must not |
|-------|-------------------|--------------|
| 0 | docs, AGENTS, rules | application code |
| 1 | `src/umbraco/` | Next.js, observability |
| 2 | `src/web/` | observability |
| 3 | `load/k6/` | K8s obs stack |
| 4 | `deployment/k8s/apps/` | observability namespace |
| 5 | `deployment/k8s/observability/` | app OTel/Faro |
| 6 | OTel, Alloy, Faro, Pyroscope SDKs | Beyla (unless user asks) |
| 7 | dashboards, alerts, demo docs | — |
| 8 | Beyla | — |

## Key paths (when they exist)

- CMS: `src/umbraco/`
- Web: `src/web/`
- k6: `load/k6/`
- SQL K8s: `deployment/k8s/sqlserver/`
- Apps K8s: `deployment/k8s/apps/`
- Obs K8s: `deployment/k8s/observability/`
- Alloy/Grafana as code: `observability/`

## Specs

- App features: [docs/APPLICATION_SPEC.md](../../../docs/APPLICATION_SPEC.md)
- Architecture: [docs/ARCHITECTURE.md](../../../docs/ARCHITECTURE.md)
- Monitoring: [docs/MONITORING_STACK.md](../../../docs/MONITORING_STACK.md)
- k6: [docs/K6_LOAD_TESTING.md](../../../docs/K6_LOAD_TESTING.md)

## Observability naming

- `service.name`: `umbraco-cms`, `umbraco-web`
- `deployment.environment`: `demo`
- No high-cardinality metric labels (slugs, user ids)

## External skills

- .NET CI: `gh-actions-dotnet-build`
- Node CI: `gh-actions-nodejs-build`
- Pipelines: `ci-cd-pipeline-foundation`
- shadcn UI: plugin skill `shadcn`

## After cross-cutting changes

Update [AGENTS.md](../../../AGENTS.md) per `agents-md-sync` skill.

## Done means

Phase DoD checkboxes in IMPLEMENTATION_PLAN are satisfied; k6 smoke passes when the phase requires it.
