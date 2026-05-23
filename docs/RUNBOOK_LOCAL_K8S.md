# Runbook: local Kubernetes (apps)

**Status:** Phase 3 complete. Umbraco + frontend deployed via CI to namespace `demo-services`; CMS `https://demo-cms.1node.tech`, web `https://demo-umbr-fe.1node.tech` (nginx SSL in front of cluster).

## Prerequisites

- `kubectl` configured for the remote demo cluster
- SQL Server applied ([deployment/k8s/sqlserver/README.md](../deployment/k8s/sqlserver/README.md))
- Traefik with `IngressRoute` CRD (entryPoint `web`)
- Optional local ingress hosts: `web.demo.local`, `cms.demo.local`

## Deploy order

1. `kubectl apply -f deployment/k8s/sqlserver/`
2. `kubectl apply -f deployment/k8s/apps/namespace.yaml`
3. Umbraco: [`.github/workflows/umbraco-container.yml`](../.github/workflows/umbraco-container.yml) or [deployment/k8s/apps/umbraco/README.md](../deployment/k8s/apps/umbraco/README.md) (`-n demo-services`)
4. Frontend: [`.github/workflows/umbr-fe-container.yml`](../.github/workflows/umbr-fe-container.yml) or [deployment/k8s/apps/umbr-fe/README.md](../deployment/k8s/apps/umbr-fe/README.md) (`-n demo-services`)

## Verify (CMS)

- `kubectl -n demo-services get pods -l app.kubernetes.io/name=demo-umbraco`

**bash / Git Bash / WSL:**

```bash
curl -sS https://demo-cms.1node.tech/health
curl -sS https://demo-cms.1node.tech/ready
curl -sS 'https://demo-cms.1node.tech/umbraco/delivery/api/v2/content?take=1'
```

**PowerShell** (`curl` is `Invoke-WebRequest`; use `curl.exe` or):

```powershell
curl.exe -sS https://demo-cms.1node.tech/health
curl.exe -sS https://demo-cms.1node.tech/ready
Invoke-RestMethod 'https://demo-cms.1node.tech/umbraco/delivery/api/v2/content?take=1'
```
- k6 CMS-only (**Phase 6**): `K6_CMS_BASE_URL=https://demo-cms.1node.tech` — see [K6_LOAD_TESTING.md](./K6_LOAD_TESTING.md)

## Port-forward (no Ingress)

```bash
kubectl -n demo-services port-forward svc/demo-umbraco 8080:8080
```

## Verify (frontend)

- `kubectl -n demo-services get pods -l app.kubernetes.io/name=demo-umbr-fe`

```bash
curl -sS https://demo-umbr-fe.1node.tech/api/health
curl -sS -o /dev/null -w "%{http_code}\n" https://demo-umbr-fe.1node.tech/
```

```powershell
curl.exe -sS https://demo-umbr-fe.1node.tech/api/health
```

k6 (**Phase 6**, full suite): `K6_WEB_BASE_URL=https://demo-umbr-fe.1node.tech` — see [K6_LOAD_TESTING.md](./K6_LOAD_TESTING.md). Phase 3 was accepted with `curl` health checks only (no k6).

Port-forward:

```bash
kubectl -n demo-services port-forward svc/demo-umbr-fe 3000:3000
```
