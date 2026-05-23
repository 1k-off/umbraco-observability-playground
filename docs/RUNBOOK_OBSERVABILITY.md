# Runbook: observability stack

**Status:** draft — complete in **Phase 4** when `deployment/k8s/observability/` is deployed.

## Namespace

`observability` — Grafana, Mimir, Loki, Tempo, Pyroscope, Alloy (see [MONITORING_STACK.md](./MONITORING_STACK.md)).

## Access Grafana

- Ingress (preferred for demo), or
- `kubectl -n observability port-forward svc/grafana 3000:80`

Admin credentials: Kubernetes Secret (not in git). Document secret name and rotation in the observability README under `deployment/k8s/observability/`.

## Alloy OTLP gateway (Phase 5)

Apps send OTLP to:

```text
alloy-gateway.observability.svc:4317
```

Confirm Service name and ports after Helm install; update this section and [MONITORING_STACK.md](./MONITORING_STACK.md).

## Health checks

- Grafana → **Connections**: Mimir, Loki, Tempo datasources healthy
- Alloy DaemonSet pods running
- Pyroscope UI reachable (port-forward or ingress)

## Phase 4 vs 5

Phase 4 deploys the stack with **placeholder** Alloy config. Application OTLP and Faro are wired in **Phase 5**.
