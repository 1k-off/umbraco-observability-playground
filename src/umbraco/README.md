# Umbraco CMS (17.4.2)

**Phase 1 complete.** Headless Umbraco for the monitoring demo: **Delivery API**, demo content seed, search and health endpoints.

| Environment | URL |
|-------------|-----|
| Production (K8s) | `https://demo-cms.1node.tech` |
| Local Podman | `http://localhost:8080` |

Built and run with **Podman** only (no local .NET SDK required). K8s: [deployment/k8s/apps/umbraco/README.md](../../deployment/k8s/apps/umbraco/README.md).

## Stack

| Item | Value |
|------|--------|
| Umbraco | 17.4.2 ([templates](https://docs.umbraco.com/umbraco-cms/get-started/installation)) |
| .NET | 10.0 |
| SQL Server | Remote `10.255.0.101:1433` (see `deployment/k8s/sqlserver/`) |

## Build image

From this directory:

```bash
podman build -f Containerfile -t umbraco-monitoring-cms:latest .
```

Or from repo root:

```powershell
.\scripts\podman-umbraco-build.ps1
```

### CI image (GitHub Packages)

Workflow [`.github/workflows/umbraco-container.yml`](../../.github/workflows/umbraco-container.yml) builds and pushes a **public** image on push to `main` / `master`:

```text
ghcr.io/<github-owner>/umbraco-monitoring-cms:latest
ghcr.io/<github-owner>/umbraco-monitoring-cms:<git-sha>
```

Pull (no auth for public packages):

```bash
podman pull ghcr.io/<github-owner>/umbraco-monitoring-cms:latest
```

## Run

Set SQL password (match `mssql2025-secret`):

```powershell
$env:ConnectionStrings__umbracoDbDSN = "Server=10.255.0.101,1433;Database=Umbraco;User Id=sa;Password=YOUR_PASSWORD;TrustServerCertificate=True;Encrypt=True"

podman run --rm -p 8080:8080 `
  -e ASPNETCORE_ENVIRONMENT=Docker `
  -e ASPNETCORE_URLS=http://0.0.0.0:8080 `
  -e Umbraco__CMS__Global__UseHttps=false `
  -e ConnectionStrings__umbracoDbDSN=$env:ConnectionStrings__umbracoDbDSN `
  -e Umbraco__CMS__Unattended__UnattendedUserPassword="ChangeMe_Demo123!" `
  umbraco-monitoring-cms:latest
```

Or:

```powershell
.\scripts\podman-umbraco-run.ps1 -SqlPassword "YOUR_PASSWORD"
```

First start runs **unattended install** and **content seed** (100 articles, 25 media images from `seed-media/`). Allow 2–5 minutes.

Copy `appsettings.Development.json.example` → `appsettings.Development.json` (gitignored) for local SDK/`dotnet run`.

## Endpoints

| URL | Description |
|-----|-------------|
| http://localhost:8080/health | Liveness |
| http://localhost:8080/ready | Readiness (SQL) |
| http://localhost:8080/umbraco/delivery/api/v2/ | Delivery API |
| http://localhost:8080/umbraco/api/search?q=article | Examine search |
| Response headers | `X-Request-Id` on all requests; same value in structured logs as `requestId` |
| http://localhost:8080/umbraco/api/demo/slow | Feature flag `Demo:SlowEndpoint` |
| http://localhost:8080/umbraco | Backoffice (after install) |

## Develop without local SDK

```bash
podman run --rm -v "./src/umbraco:/src:Z" -w /src -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Development \
  -e ASPNETCORE_URLS=http://0.0.0.0:8080 \
  -e "ConnectionStrings__umbracoDbDSN=Server=10.255.0.101,1433;Database=Umbraco;User Id=sa;Password=YOUR_PASSWORD;TrustServerCertificate=True;Encrypt=True" \
  -e Umbraco__CMS__Unattended__UnattendedUserPassword="ChangeMe_Demo123!" \
  mcr.microsoft.com/dotnet/sdk:10.0 \
  bash -c "dotnet run --project /src/UmbracoMonitoring.csproj --no-launch-profile"
```

`ASPNETCORE_URLS` must bind `0.0.0.0`, not `localhost`, or port mapping from the host will not reach Kestrel. `--no-launch-profile` avoids `Properties/launchSettings.json` overriding URLs inside the container.

Backoffice over HTTP requires `Umbraco:CMS:Global:UseHttps` = `false` (set in `appsettings.Development.json` / `appsettings.Docker.json`, or `-e Umbraco__CMS__Global__UseHttps=false`). Without it, OpenIddict returns `ID2083` (HTTPS only).
