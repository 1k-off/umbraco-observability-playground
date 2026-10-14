# Observability stack (Kubernetes)

**Phases 4–5 ✅** — LGTM+ in namespace `observability`; apps send OTLP/Faro/profiles via `alloy-gateway`. NetworkPolicies are not used in this demo.

## Architecture

```text
demo-services (apps)          observability
┌─────────────────┐          ┌──────────────────────────────────────┐
│ umbraco, umbr-fe│          │ MinIO (S3) ← Mimir, Loki, Tempo      │
└────────┬────────┘          │ OpenMaxIO Console → MinIO UI         │
         │ scrape/logs       │ Grafana ← datasources                │
         │ OTLP/Faro/profile │ Alloy DaemonSet + alloy-gateway:4317 │
         └──────────────────►│ Pyroscope                            │
                             └──────────────────────────────────────┘
```

## Prerequisites

- `kubectl` with access to the demo cluster
- **Helm 3.2+**
- StorageClass for PVCs (MinIO 20Gi, Loki/Tempo/Pyroscope/Grafana 5–10Gi each)
- Traefik `IngressRoute` CRD (same as [apps](../apps/))
- Cluster capacity: **≥ 8 GiB RAM** free for the stack (demo sizing; tune for production)

## Secrets (before first deploy)

Edit placeholders — **do not commit real credentials**:

| File | Keys |
|------|------|
| [minio/secret.yaml](./minio/secret.yaml) | `MINIO_ROOT_USER`, `MINIO_ROOT_PASSWORD` |
| [openmaxio/secret.yaml](./openmaxio/secret.yaml) | `CONSOLE_PBKDF_PASSPHRASE`, `CONSOLE_PBKDF_SALT` |
| [grafana/secret.yaml](./grafana/secret.yaml) | `admin-user`, `admin-password` |

Or create via kubectl:

```bash
kubectl create secret generic minio-credentials -n observability \
  --from-literal=MINIO_ROOT_USER='demo-minio' \
  --from-literal=MINIO_ROOT_PASSWORD='...'

kubectl create secret generic grafana-admin -n observability \
  --from-literal=admin-user=admin \
  --from-literal=admin-password='...'

kubectl create secret generic openmaxio-console -n observability \
  --from-literal=CONSOLE_PBKDF_PASSPHRASE='...' \
  --from-literal=CONSOLE_PBKDF_SALT='...'
```

## Quick install

From repo root (Linux/macOS/WSL):

```bash
chmod +x deployment/k8s/observability/install.sh
./deployment/k8s/observability/install.sh
```

Manual steps equivalent to the script:

```bash
kubectl apply -f deployment/k8s/observability/namespace.yaml
kubectl apply -f deployment/k8s/observability/minio/ -n observability
kubectl apply -f deployment/k8s/observability/grafana/secret.yaml -n observability
# wait for MinIO, then bucket job
kubectl apply -f deployment/k8s/observability/minio/job-buckets.yaml -n observability
kubectl apply -f deployment/k8s/observability/openmaxio/ -n observability

helm repo add grafana https://grafana.github.io/helm-charts && helm repo update

helm upgrade --install mimir grafana/mimir-distributed -n observability \
  -f deployment/k8s/observability/helm/values-mimir.yaml --wait

helm upgrade --install loki grafana/loki -n observability \
  -f deployment/k8s/observability/helm/values-loki.yaml --wait

helm repo add grafana-community https://grafana-community.github.io/helm-charts
helm upgrade --install tempo grafana-community/tempo -n observability \
  -f deployment/k8s/observability/helm/values-tempo.yaml --wait

helm upgrade --install pyroscope grafana/pyroscope -n observability \
  -f deployment/k8s/observability/helm/values-pyroscope.yaml --wait

helm upgrade --install grafana grafana-community/grafana -n observability \
  -f deployment/k8s/observability/helm/values-grafana.yaml --wait

helm upgrade --install alloy grafana/alloy -n observability \
  -f deployment/k8s/observability/helm/values-alloy-daemonset.yaml --wait

helm upgrade --install alloy-gateway grafana/alloy -n observability \
  -f deployment/k8s/observability/helm/values-alloy-gateway.yaml --wait

kubectl apply -f deployment/k8s/observability/alloy/gateway-service.yaml -n observability
kubectl apply -f deployment/k8s/observability/grafana/ingressroute.yaml -n observability
```

## Helm releases

| Release | Chart | Values |
|---------|-------|--------|
| `mimir` | `grafana/mimir-distributed` | [helm/values-mimir.yaml](./helm/values-mimir.yaml) |
| `loki` | `grafana/loki` | [helm/values-loki.yaml](./helm/values-loki.yaml) |
| `tempo` | `grafana-community/tempo` | [helm/values-tempo.yaml](./helm/values-tempo.yaml) |
| `pyroscope` | `grafana/pyroscope` | [helm/values-pyroscope.yaml](./helm/values-pyroscope.yaml) |
| `grafana` | `grafana-community/grafana` | [helm/values-grafana.yaml](./helm/values-grafana.yaml) |
| `alloy` | `grafana/alloy` (DaemonSet) | [helm/values-alloy-daemonset.yaml](./helm/values-alloy-daemonset.yaml) |
| `alloy-gateway` | `grafana/alloy` (Deployment) | [helm/values-alloy-gateway.yaml](./helm/values-alloy-gateway.yaml) |

Pin chart versions in production by adding `--version <x.y.z>` to each `helm upgrade --install`.

## Internal URLs (ClusterIP)

| Service | URL | Purpose |
|---------|-----|---------|
| `mimir-gateway` | `http://mimir-gateway.observability.svc:80/prometheus` | PromQL / remote_write |
| `loki-gateway` | `http://loki-gateway.observability.svc:80` | LogQL push |
| `tempo` | `http://tempo.observability.svc:3200` | Trace query |
| `pyroscope` | `http://pyroscope.observability.svc:4040` | Profiles |
| `alloy-gateway` | `alloy-gateway.observability.svc:4317` (gRPC), `:4318` (HTTP) | OTLP (Phase 5) |
| `minio` | `minio.observability.svc:9000` | S3 API |

## Public access

| URL | Component |
|-----|-----------|
| `https://demo-grafana.1node.tech` | Grafana UI ([ingressroute.yaml](./grafana/ingressroute.yaml)) |
| `https://demo-openmaxio.1node.tech` | OpenMaxIO Console — MinIO buckets/objects UI ([openmaxio/](./openmaxio/)) |

Configure DNS + nginx TLS the same way as CMS/web ([apps/umbraco/README.md](../apps/umbraco/README.md)).

**OpenMaxIO login:** same as MinIO — `MINIO_ROOT_USER` / `MINIO_ROOT_PASSWORD` from Secret `minio-credentials`. See [openmaxio/README.md](./openmaxio/README.md).

Port-forward (no ingress):

```bash
kubectl -n observability port-forward svc/grafana 3000:80
# http://localhost:3000 — login with grafana-admin secret
```

Pyroscope UI:

```bash
kubectl -n observability port-forward svc/pyroscope 4040:4040
```

OpenMaxIO Console:

```bash
kubectl -n observability port-forward svc/openmaxio 9002:9002
# http://localhost:9002 — login with minio-credentials
```

## Alloy config (Phase 5 ✅)

Full pipeline in [observability/alloy/config.alloy](../../../observability/alloy/config.alloy) — mirrored in [helm/values-alloy-gateway.yaml](./helm/values-alloy-gateway.yaml) and [values-alloy-daemonset.yaml](./helm/values-alloy-daemonset.yaml).

## Verify (Phase 4 DoD)

```bash
kubectl -n observability get pods
kubectl -n observability wait --for=condition=ready pod -l app.kubernetes.io/name=grafana --timeout=300s

# Grafana health
kubectl -n observability port-forward svc/grafana 3000:80 &
curl -sS http://localhost:3000/api/health

# Datasources (needs admin password from secret)
curl -u "admin:$(kubectl -n observability get secret grafana-admin -o jsonpath='{.data.admin-password}' | base64 -d)" \
  http://localhost:3000/api/datasources

# Alloy gateway OTLP port
kubectl -n observability get svc alloy-gateway
kubectl -n observability get pods -l app.kubernetes.io/instance=alloy
kubectl -n observability get pods -l app.kubernetes.io/instance=alloy-gateway
```

In Grafana UI → **Connections** → **Data sources**: Mimir, Loki, Tempo should show **Working**. Pyroscope datasource requires the Pyroscope app plugin (installed via Helm values).

## Dashboards + cluster metrics (Phase 7)

```powershell
# Sidecar ConfigMaps (JSON in observability/grafana/dashboards/)
powershell -File ./scripts/apply-grafana-dashboards.ps1

# node-exporter + kube-state-metrics + Alloy RBAC/config refresh
powershell -File ./scripts/install-cluster-metrics.ps1
```

Helm Grafana must have `sidecar.dashboards.enabled: true` ([helm/values-grafana.yaml](./helm/values-grafana.yaml)).

## Uninstall

```bash
./deployment/k8s/observability/uninstall.sh
```

PVCs in `observability` may remain; delete manually if needed.

## Related docs

- [docs/RUNBOOK_OBSERVABILITY.md](../../../docs/RUNBOOK_OBSERVABILITY.md)
- [docs/MONITORING_STACK.md](../../../docs/MONITORING_STACK.md)
- [docs/IMPLEMENTATION_PLAN.md](../../../docs/IMPLEMENTATION_PLAN.md) — Phase 4 DoD
