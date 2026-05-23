# SQL Server (Kubernetes)

SQL Server 2025 for the Umbraco demo. Namespace: `sqlserver`. In-cluster DNS: `mssql2025.sqlserver.svc.cluster.local:1433`.

## External access

The Service uses **ClusterIP** with `externalIPs: [10.255.0.101]` so clients outside the cluster (local Podman, Docker Compose) can use:

```text
Server=10.255.0.101,1433;Database=Umbraco;User Id=sa;Password=<SA_PASSWORD>;TrustServerCertificate=True;Encrypt=True
```

MetalLB / Traefik on the cluster is configured separately (not in this repo). Reserve `10.255.0.101` per your cluster design — see comments in [service.yaml](./service.yaml).

## Apply

1. Set `SA_PASSWORD` in [secret.yaml](./secret.yaml) (or create the Secret with `kubectl`; do not commit real passwords).
2. From repo root:

```bash
kubectl apply -f deployment/k8s/sqlserver/namespace.yaml
kubectl apply -f deployment/k8s/sqlserver/secret.yaml
kubectl apply -f deployment/k8s/sqlserver/statefulset.yaml
kubectl apply -f deployment/k8s/sqlserver/service.yaml
```

3. Wait for the pod:

```bash
kubectl -n sqlserver get pods -w
```

## Verify

```bash
kubectl -n sqlserver exec -it statefulset/mssql2025 -- /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P '<SA_PASSWORD>' -C -Q "SELECT 1"
```

(Adjust `sqlcmd` path for your image version if the command fails.)

## Connection from Umbraco

| Where | Server |
|-------|--------|
| Podman / host on LAN | `10.255.0.101,1433` |
| CMS pod in `demo-services` | `mssql2025.sqlserver.svc.cluster.local,1433` |

See [src/umbraco/README.md](../../../src/umbraco/README.md) and [.env.example](../../../.env.example).
