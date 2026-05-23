# Umbraco CMS (Kubernetes)

**Phase 3 complete.** Public URL: `https://demo-cms.1node.tech` (nginx SSL → cluster). Image: `ghcr.io/1k-off/monitoring-demo-umbraco:latest`.

Manifests have **no `namespace`** — set it at apply time, e.g. `-n demo-services` (see [../namespace.yaml](../namespace.yaml)).

## Prerequisites

- SQL Server in namespace `sqlserver` ([../../sqlserver/README.md](../../sqlserver/README.md))
- Traefik with CRD `IngressRoute` (`traefik.io/v1alpha1`); entryPoint `web` (optional; use port-forward without route)
- `kubectl` configured for the cluster

## CI/CD (GitHub Actions)

Workflow [`.github/workflows/umbraco-container.yml`](../../../.github/workflows/umbraco-container.yml): `docker/metadata-action` + `build-push` (tags: branch, PR, `sha`, `latest` on default branch) → deploy on `main` only (PR = build, no deploy).

Uses `azure/k8s-create-secret@v5` and `Azure/k8s-deploy@v5` with `secrets.KUBECONFIG`. Deploy substitutes `ghcr.io/<owner>/monitoring-demo-umbraco:<sha>` (`needs.build.outputs.image_tag` = commit SHA from metadata).

**Repository secrets:**

| Secret | Purpose |
|--------|---------|
| `KUBECONFIG` | Kubeconfig for `azure/k8s-set-context` |
| `UMBRACO_CONNECTION_STRING` | `ConnectionStrings__umbracoDbDSN` |

**Workflow `env`:** `K8S_NAMESPACE`, `DEPLOY_URL` (must match [ingressroute.yaml](./ingressroute.yaml) host).

Do not apply [secret.yaml](./secret.yaml) with placeholders — CI creates `demo-umbraco-secret`.

## Before apply

1. Edit [secret.yaml](./secret.yaml):
   - `REPLACE_SA_PASSWORD` — same as `mssql2025-secret` in `sqlserver`
2. If the host is not `cms.demo.local`, update [configmap.yaml](./configmap.yaml) `Umbraco__CMS__WebRouting__UmbracoApplicationUrl` and the `Host()` rule in [ingressroute.yaml](./ingressroute.yaml).
3. If your Traefik entryPoint is not `web`, change `spec.entryPoints` in `ingressroute.yaml` (e.g. `websecure` for HTTPS).

## HTTPS in front of the cluster (nginx / SSL termination)

Public URL must match [configmap.yaml](./configmap.yaml): `Umbraco__CMS__WebRouting__UmbracoApplicationUrl` uses **`https://`** and `Umbraco__CMS__Global__UseHttps` is `true`. The pod listens on HTTP; Kestrel learns HTTPS from `X-Forwarded-Proto`.

On the **nginx** VM (or any reverse proxy in front of K8s), forward headers:

```nginx
proxy_set_header Host $host;
proxy_set_header X-Forwarded-Proto $scheme;
proxy_set_header X-Forwarded-Host $host;
proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
```

**Backoffice login** `error:invalid_request` / [OpenIddict ID2043](https://documentation.openiddict.com/errors/ID2043): redirect URI in the request does not match `umbracoOpenIddictApplications.RedirectUris` (often `http` vs `https`, or dev `localhost` only). After fixing the URL above, restart the deployment so Umbraco can register `https://<host>/umbraco/oauth_complete`. If login still fails, inspect SQL:

```sql
SELECT ClientId, RedirectUris, PostLogoutRedirectUris
FROM umbracoOpenIddictApplication
WHERE ClientId = 'umbraco-back-office';
```

## Deploy

From repo root (`<namespace>` e.g. `demo-services`):

```bash
kubectl apply -f deployment/k8s/apps/umbraco/secret.yaml -n <namespace>
kubectl apply -f deployment/k8s/apps/umbraco/configmap.yaml -n <namespace>
kubectl apply -f deployment/k8s/apps/umbraco/pvc.yaml -n <namespace>
kubectl apply -f deployment/k8s/apps/umbraco/deployment.yaml -n <namespace>
kubectl apply -f deployment/k8s/apps/umbraco/service.yaml -n <namespace>
kubectl apply -f deployment/k8s/apps/umbraco/ingressroute.yaml -n <namespace>
```

Or one shot:

```bash
kubectl apply -f deployment/k8s/apps/umbraco/ -n <namespace>
```

**First start:** unattended install + content seed can take **2–5 minutes**. `startupProbe` allows up to ~10 minutes.

## Verify

```bash
kubectl -n <namespace> get pods,svc,ingressroute
kubectl -n <namespace> wait --for=condition=ready pod -l app.kubernetes.io/name=demo-umbraco --timeout=600s
```

With Traefik route (`cms.demo.local` in `/etc/hosts` or DNS, traffic to Traefik `:80`):

```bash
curl -sS http://cms.demo.local/health
curl -sS http://cms.demo.local/ready
curl -sS "http://cms.demo.local/umbraco/delivery/api/v2/content?take=3"
```

Port-forward (no IngressRoute):

```bash
kubectl -n <namespace> port-forward svc/demo-umbraco 8080:8080
curl -sS http://localhost:8080/health
```

## Notes

- **Replicas: 1** — Umbraco MainDom; do not scale horizontally without shared storage and Umbraco load-balancing guidance.
- **PVC** — `wwwroot/media` and `umbraco/Data` (Examine/NuCache) persist across pod restarts.
- **Strategy Recreate** — required with `ReadWriteOnce` PVC.
- **Probes:** liveness `/health`, readiness `/ready` (includes SQL check).
- Re-seed with media requires empty DB or removed demo content; see [src/umbraco/README.md](../../../src/umbraco/README.md).

## Files

| File | Purpose |
|------|---------|
| `secret.yaml` | SQL connection string, unattended password |
| `configmap.yaml` | ASP.NET / Umbraco env (non-secret) |
| `pvc.yaml` | Media + local Umbraco data |
| `deployment.yaml` | Pod spec, probes, resources |
| `service.yaml` | ClusterIP `:8080` |
| `ingressroute.yaml` | Traefik `IngressRoute` + `Middleware` (upload limit), host `cms.demo.local` |
