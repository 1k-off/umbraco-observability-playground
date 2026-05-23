# Deployment

Kubernetes manifests for the umbraco-monitoring demo. Apply with plain `kubectl apply -f` (no Kustomize).

| Path | Namespace | Description |
|------|-----------|-------------|
| [k8s/sqlserver/](./k8s/sqlserver/) | `sqlserver` | SQL Server 2025, LB IP `10.255.0.101` (cluster MetalLB) |
| k8s/apps/ | `demo-services` | Umbraco + Next.js (Phase 3 ✅) |
| k8s/observability/ | `observability` | Grafana LGTM+ stack (Phase 4 ← current) |

Deploy SQL Server first. Cluster load-balancer (MetalLB pools, etc.) is **not** in this repo — configure on the cluster before applying `service.yaml`.
