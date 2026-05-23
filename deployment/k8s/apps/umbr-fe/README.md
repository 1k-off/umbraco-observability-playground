# Next.js frontend (`umbr-fe`, Kubernetes)

**Phase 3 complete.** Public URL: `https://demo-umbr-fe.1node.tech` (nginx SSL → cluster, same pattern as CMS). Image: `ghcr.io/<owner>/monitoring-demo-umbr-fe:<commit-sha>`.

Manifests have **no `namespace`** — set at apply time, e.g. `-n demo-services` (same as [../umbraco/README.md](../umbraco/README.md)).

## Prerequisites

- Umbraco CMS reachable at `https://demo-cms.1node.tech` (or update [configmap.yaml](./configmap.yaml) `CMS_BASE_URL`)
- Traefik with `IngressRoute` CRD; entryPoint `web`
- `kubectl` configured for the cluster

## CI/CD (GitHub Actions)

Workflow [`.github/workflows/umbr-fe-container.yml`](../../../.github/workflows/umbr-fe-container.yml): Docker build with `CMS_BASE_URL` build-arg → GHCR tags (`<sha>`, branch, `pr-<n>`) → deploy on `main` (PR = build only).

Uses `Azure/k8s-deploy@v5` with `secrets.KUBECONFIG`. **K8s always gets the immutable commit SHA**, not branch/PR tags. Deploy replaces placeholder `0.0.0` in [deployment.yaml](./deployment.yaml).

**Repository secrets:**

| Secret | Purpose |
|--------|---------|
| `KUBECONFIG` | Kubeconfig for `azure/k8s-set-context` |

**Workflow `env`:** `K8S_NAMESPACE`, `CMS_BASE_URL`, `DEPLOY_URL` (must match [configmap.yaml](./configmap.yaml) and [ingressroute.yaml](./ingressroute.yaml)).

No Kubernetes Secret required for the web app (non-sensitive env in ConfigMap).

## Before apply

1. If the public host is not `demo-umbr-fe.1node.tech`, update:
   - [ingressroute.yaml](./ingressroute.yaml) `Host(...)`
   - [configmap.yaml](./configmap.yaml) `NEXT_PUBLIC_SITE_URL`
   - workflow `DEPLOY_URL` in [`.github/workflows/umbr-fe-container.yml`](../../../.github/workflows/umbr-fe-container.yml)
2. Point **nginx** (or DNS) at the cluster for the web host, same as CMS.
3. If Traefik entryPoint is not `web`, change `spec.entryPoints` in `ingressroute.yaml`.

## Deploy

From repo root:

```bash
kubectl apply -f deployment/k8s/apps/umbr-fe/ -n demo-services
```

Or rely on GitHub Actions after push to `main`.

**Rollout:**

```bash
kubectl -n demo-services rollout status deployment/demo-umbr-fe --timeout=300s
```

## Verify

```bash
kubectl -n demo-services get pods,svc,hpa,ingressroute -l app.kubernetes.io/name=demo-umbr-fe
```

With Traefik route (`demo-umbr-fe.1node.tech` in DNS → nginx → cluster):

```bash
curl -sS https://demo-umbr-fe.1node.tech/api/health
curl -sS -o /dev/null -w "%{http_code}\n" https://demo-umbr-fe.1node.tech/
curl -sS "https://demo-umbr-fe.1node.tech/api/articles?take=1"
```

Port-forward (no IngressRoute):

```bash
kubectl -n demo-services port-forward svc/demo-umbr-fe 3000:3000
curl -sS http://localhost:3000/api/health
```

## Notes

- **Probes:** `/api/health` (liveness, readiness, startup).
- **HPA:** CPU 70%, min 2 / max 5 pods ([hpa.yaml](./hpa.yaml)); Deployment starts with 2 replicas.
- **`CMS_BASE_URL`:** public CMS URL so SSR and `next/image` media URLs work in the browser. Server-side BFF calls the same origin.
- **Build-time `CMS_BASE_URL`:** must match CMS hostname for `next.config.ts` `images.remotePatterns` (set in CI Docker build-arg).
- Observability `service.name` in Phase 5: `umbraco-web` ([docs/MONITORING_STACK.md](../../../docs/MONITORING_STACK.md)).

## Files

| File | Purpose |
|------|---------|
| `configmap.yaml` | `CMS_BASE_URL`, `NEXT_PUBLIC_SITE_URL`, `PORT` |
| `deployment.yaml` | Pod spec, probes, resources |
| `service.yaml` | ClusterIP `:3000` |
| `ingressroute.yaml` | Traefik host `demo-umbr-fe.1node.tech` |
| `hpa.yaml` | Horizontal pod autoscaling (optional for demo; applied by CI) |
