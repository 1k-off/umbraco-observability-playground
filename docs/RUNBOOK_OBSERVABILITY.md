# Runbook: observability stack

**Status:** Phase 4 ✅ complete (LGTM+ in cluster). Install/upgrade: [deployment/k8s/observability/README.md](../deployment/k8s/observability/README.md).

## Namespace

`observability` — Grafana, Mimir, Loki, Tempo, Pyroscope, Alloy, MinIO, OpenMaxIO Console.

## Access Grafana

| Method | URL |
|--------|-----|
| Ingress (demo) | `https://demo-grafana.1node.tech` |
| Port-forward | `kubectl -n observability port-forward svc/grafana 3000:80` → http://localhost:3000 |

Admin credentials: Secret `grafana-admin` (`admin-user`, `admin-password`). See [deployment/k8s/observability/grafana/secret.yaml](../deployment/k8s/observability/grafana/secret.yaml).

```bash
kubectl -n observability get secret grafana-admin -o jsonpath='{.data.admin-password}' | base64 -d
```

## Access OpenMaxIO (MinIO Console)

Community UI for MinIO S3 — [OpenMaxIO/openmaxio-object-browser](https://github.com/OpenMaxIO/openmaxio-object-browser) / [huncrys/minio-console](https://github.com/huncrys/minio-console).

| Method | URL |
|--------|-----|
| Ingress (demo) | `https://demo-openmaxio.1node.tech` |
| Port-forward | `kubectl -n observability port-forward svc/openmaxio 9002:9002` → http://localhost:9002 |

Login: **MinIO credentials** from Secret `minio-credentials` (`MINIO_ROOT_USER`, `MINIO_ROOT_PASSWORD`).

Console session encryption: Secret `openmaxio-console` (`CONSOLE_PBKDF_PASSPHRASE`, `CONSOLE_PBKDF_SALT`) — see [openmaxio/secret.yaml](../deployment/k8s/observability/openmaxio/secret.yaml).

## Internal service endpoints

| Backend | Cluster DNS |
|---------|-------------|
| Mimir (Prometheus API) | `http://mimir-gateway.observability.svc:80/prometheus` |
| Mimir remote_write | `http://mimir-gateway.observability.svc:80/api/v1/push` |
| Loki | `http://loki-gateway.observability.svc:80` |
| Tempo | `http://tempo.observability.svc:3200` |
| Pyroscope | `http://pyroscope.observability.svc:4040` |

## Alloy OTLP gateway (Phase 5 apps)

Applications send OTLP to:

```text
alloy-gateway.observability.svc:4317   # gRPC
alloy-gateway.observability.svc:4318   # HTTP
```

Phase 4: gateway listens on OTLP; signals dropped until Phase 5. Phase 5: update Alloy config from [observability/alloy/config.alloy](../observability/alloy/config.alloy).

## Health checks

1. **Pods:** `kubectl -n observability get pods` — all Running/Completed (MinIO bucket job).
2. **Grafana:** `curl http://localhost:3000/api/health` (via port-forward).
3. **Connections:** Grafana → **Connections** → **Data sources** — Mimir, Loki, Tempo healthy.
4. **Alloy:** `kubectl -n observability get pods -l app.kubernetes.io/instance=alloy` and `...=alloy-gateway`.
5. **Pyroscope:** port-forward `svc/pyroscope 4040:4040` or query via Grafana Pyroscope datasource.

## Install / upgrade

```bash
./deployment/k8s/observability/install.sh
```

**Tempo** and **Grafana** use [grafana-community/helm-charts](https://github.com/grafana-community/helm-charts) (`grafana-community/tempo`, `grafana-community/grafana`), not deprecated charts in `grafana/helm-charts`. Repo: `helm repo add grafana-community https://grafana-community.github.io/helm-charts`.

Upgrade single component:

```bash
helm upgrade mimir grafana/mimir-distributed -n observability \
  -f deployment/k8s/observability/helm/values-mimir.yaml
```

## Phase 4 vs 5

| Phase | Scope |
|-------|--------|
| **4** ✅ | LGTM+ infra, placeholder Alloy, Grafana datasources |
| **5** | OTel in Umbraco/Next.js, Faro, full Alloy River config, profiles push |

### Phase 5 rollout (after merging instrumentation)

1. **Alloy** — upgrade gateway + DaemonSet with River config from [observability/alloy/](../../observability/alloy/):

```powershell
helm upgrade alloy-gateway grafana/alloy -n observability `
  -f deployment/k8s/observability/helm/values-alloy-gateway.yaml --wait
helm upgrade alloy grafana/alloy -n observability `
  -f deployment/k8s/observability/helm/values-alloy-daemonset.yaml --wait
kubectl apply -f deployment/k8s/observability/alloy/gateway-service.yaml
```

2. **Apps** — rebuild/push CMS and web images (OTel + Pyroscope + Faro), then apply ConfigMaps and roll Deployments in `demo-services`.

3. **Verify** (Grafana Explore) — see [Explore traps](#grafana-explore-phase-5-traps) for filters that avoid probe/SQL noise:
   - Tempo web: `{ resource.service.name = "umbraco-web" && name = "cms.fetch" }`
   - Tempo CMS: `{ resource.service.name = "umbraco-cms" && span.http.route != "" }`
   - Loki CMS: `{service_name="demo-umbraco"} |= "trace_id"` (or `TraceId`)
   - Mimir: RED for **both** jobs — different metric names for .NET vs Node (below)
   - Pyroscope: `umbraco-cms` / `umbraco-web` after page loads
   - Faro: Loki `{service_name="unknown_service"} |= "sdk_name=faro-web"`; browser POST `/api/faro/collect` → 202

**Endpoints (in-cluster):**

| Signal | App env | Collector |
|--------|---------|-----------|
| OTLP gRPC | `OTEL_EXPORTER_OTLP_ENDPOINT=http://alloy-gateway.observability.svc.cluster.local:4317` | alloy-gateway:4317 |
| Faro | Browser → `/api/faro` → `FARO_COLLECTOR_UPSTREAM` | alloy-gateway:12347 |
| Pyroscope .NET | `PYROSCOPE_SERVER_ADDRESS` (native profiler in image) | pyroscope:4040 |
| Pyroscope Node | `PYROSCOPE_SERVER_ADDRESS` in `instrumentation.node.ts` | pyroscope:4040 |
| Pod logs | stdout JSON (CMS) | Alloy DaemonSet → Loki |

## Loki: which labels exist?

`loki.source.kubernetes` does **not** use OTel `service.name`. Until relabel is deployed, streams are often only:

```logql
{job="loki.source.kubernetes.demo_pods"}
```

After DaemonSet relabel (see `config-daemonset.alloy`):

```logql
{namespace="demo-services", service_name="demo-umbraco"}
{namespace="demo-services", service_name="demo-umbr-fe"}
```

CMS Serilog JSON + `trace_id` (do not require `| json` until lines appear):

```logql
{service_name="demo-umbraco"} |= "trace_id"
```

**Helm:** `values-alloy-daemonset.yaml` must match `observability/alloy/config-daemonset.alloy`. If they diverge, `helm upgrade` keeps an old inline config and `namespace`/`pod` queries return nothing.

**Diagnostics:**

```bash
kubectl -n observability get pods -l app.kubernetes.io/name=alloy
kubectl -n observability logs ds/alloy --tail=30
kubectl -n demo-services logs deploy/demo-umbraco --tail=5
```

In Grafana → Loki → Label browser: start from `job`, then `service_name` / `pod` / `namespace`.

## Labels cheat sheet (do not mix)

| Context | Label / field | Web | CMS | Faro (browser) |
|---------|----------------|-----|-----|----------------|
| Tempo / OTel resource | `resource.service.name` | `umbraco-web` | `umbraco-cms` | traces also `umbraco-web` |
| Loki pod logs (K8s relabel) | `service_name` | `demo-umbr-fe` | `demo-umbraco` | — |
| Loki Faro events (`faro.receiver`) | `service_name` | — | — | **`unknown_service`** (Alloy default) |
| Mimir OTLP → Prometheus | `job` | `demo-services/umbraco-web` | `demo-services/umbraco-cms` | — |

Pod log queries must **not** use `umbraco-web` as a Loki label. Tempo queries must **not** use `demo-umbr-fe`.

## Mimir: find real metric names

**.NET and Node export different HTTP histogram names** after OTLP → Alloy → Mimir. Filtering only `http_server_request_duration_seconds_*` hides the web.

| Service | `job` | RED counter (example) |
|---------|-------|------------------------|
| CMS (.NET) | `demo-services/umbraco-cms` | `http_server_request_duration_seconds_count` |
| Web (Node) | `demo-services/umbraco-web` | `http_server_duration_milliseconds_count` |

```promql
# CMS
sum(rate(http_server_request_duration_seconds_count{job="demo-services/umbraco-cms"}[5m]))

# Web
sum(rate(http_server_duration_milliseconds_count{job="demo-services/umbraco-web"}[5m]))

# Discovery
topk(10, count by (__name__, job) ({__name__=~"http_server.*"}))
```

OTel → Prometheus may omit `service_name` on series; prefer **`job`** from the table above.

Span metrics (optional; requires Tempo metrics generator):

```promql
sum(rate(traces_spanmetrics_calls_total{service=~"umbraco.*"}[5m])) by (service)
```

## Grafana Explore: Phase 5 traps

Symptoms below are usually **wrong filter or UI mode**, not missing instrumentation.

### Tempo — `umbraco-web` looks like “only `/api/health`”

Kubernetes **liveness/readiness** hit `/api/health` every ~10–15s and flood the trace list.

```traceql
{ resource.service.name = "umbraco-web" && span.http.route != "/api/health" }
```

SSR + CMS:

```traceql
{ resource.service.name = "umbraco-web" && name = "cms.fetch" }
{ resource.service.name = "umbraco-web" && span.http.route = "/articles/[slug]" }
```

Open the **full trace** (waterfall), not a single span row. Search type **Traces** shows the tree; span-only tables favor leaf spans.

### Tempo — `umbraco-cms` looks like “only SQL”

Umbraco background jobs emit constant **SqlClient** spans (`SELECT [umbracoDistributedJob]`, etc.). The span table shows **child** names first.

```traceql
{ resource.service.name = "umbraco-cms" && span.http.route != "" }
{ resource.service.name = "umbraco-cms" && name =~ ".*delivery.*" }
```

After browsing the site you should see routes like `umbraco/delivery/api/.../content/item/{*path}` and `umbraco/api/search`. SQL spans appear **inside** those traces.

### Mimir — “no frontend”

Web metrics use **`http_server_duration_milliseconds_*`**, not `http_server_request_duration_seconds_*`. See [Mimir](#mimir-find-real-metric-names).

### Loki — Faro (no Faro datasource)

Grafana is provisioned with Mimir, Loki, Tempo, Pyroscope only — **no Grafana Faro datasource**. RUM is viewed via **Loki** (events) and **Tempo** (browser traces).

| Check | Where |
|-------|--------|
| Collector works | Browser DevTools → `POST https://<web>/api/faro/collect` → **202** |
| Sessions, vitals, JS fetch | **Loki** (streams from `faro.receiver`) |
| Browser / navigation traces | **Tempo** (`resource.service.name = "umbraco-web"`) |

Faro lines land under **`service_name="unknown_service"`** (not `demo-umbr-fe`). Lines contain `sdk_name=faro-web`, `app_name=umbraco-web`, `session_id=...`.

```logql
{service_name="unknown_service"} |= "sdk_name=faro-web"
{service_name="unknown_service"} |= "event_name=session"
{service_name="unknown_service"} |= "faro.performance"
{service_name="unknown_service"} |= "faro.tracing"
```

Copy `traceID=...` from a Faro log line → **Tempo** → search by Trace ID.

BFF proxy spans (server-side POST to collector):

```traceql
{ resource.service.name = "umbraco-web" && span.http.target =~ ".*/api/faro.*" }
```

### Distributed tracing (browser → web → CMS → SQL)

After redeploying the web image with W3C propagation:

| Hop | Mechanism |
|-----|-----------|
| Browser → Next.js | Faro `TracingInstrumentation` adds `traceparent` on same-origin fetch/RSC |
| Next.js SSR | `@opentelemetry/instrumentation-http` continues the trace |
| Next.js → CMS | `propagation.inject` on `cmsFetch` + Undici instrumentation |
| CMS → SQL | ASP.NET Core + SqlClient instrumentation (child spans) |

**Verify in Tempo:** open one trace (exclude `/api/health`). You should see one **trace ID** with spans from `umbraco-web` and `umbraco-cms` (plus SQL children under CMS).

```traceql
{ resource.service.name = "umbraco-web" && name = "cms.fetch" }
```

Open the trace waterfall — expand CMS `GET` / Delivery API spans and SQL (`SELECT`, `UPDATE`, …) under the same trace ID.

**Limits (demo):**

- First full page load may still show a **separate** Faro browser trace and an SSR trace until navigation uses RSC fetch with `traceparent`.
- Background Umbraco SQL jobs are unrelated traces (no HTTP parent).

### Phase 5 five-minute walkthrough

1. Open `https://demo-umbr-fe.1node.tech` and two article URLs.
2. **Tempo:** `{ resource.service.name = "umbraco-web" && name = "cms.fetch" }` → open trace.
3. **Mimir:** `http_server_duration_milliseconds_count{job="demo-services/umbraco-web"}` and CMS counter above.
4. **Loki CMS:** `{service_name="demo-umbraco"} |= "trace_id"`.
5. **Loki Faro:** `{service_name="unknown_service"} |= "sdk_name=faro-web"`.
6. **Pyroscope:** app names `umbraco-cms`, `umbraco-web`.

## Troubleshooting

| Symptom | Check |
|---------|--------|
| Loki: no logs with `namespace`/`pod` | Query `{job="loki.source.kubernetes.demo_pods"}`; sync + `helm upgrade alloy` DaemonSet config |
| Loki CMS: trace filter empty | Use **`trace_id`** or substring: `{service_name="demo-umbraco"} \|= "<traceId>"`; see [run-docs/11-logs-and-trace-correlation.md](../run-docs/11-logs-and-trace-correlation.md) |
| Tempo OOM / Grafana errors | `kubectl -n observability logs statefulset/tempo --tail=50` (STS, not Deployment) |
| Web: no Mimir/Pyroscope | Next hook: `src/instrumentation.ts`; pod log should show `[obs] starting Node OTel` |
| FE Loki: fsnotify spam | Alloy `stage.drop` for `fsnotify watcher`; standard `USER nextjs` image (no entrypoint); optional `!= "fsnotify"` in Explore |
| Loki `unknown_service` + `ppppp...` | **Loki Canary** pods — `helm upgrade loki` with `lokiCanary.enabled: false`; filter `pod!~"loki-canary.*"` |
| Trace→logs button empty trace id | Tempo customQuery: `| json | TraceId="$${__trace.traceId}"`; see [run-docs/11-logs-and-trace-correlation.md](../run-docs/11-logs-and-trace-correlation.md) |
| mimir-gateway / loki-gateway nginx: `host not found in resolver "kube-dns..."` | Set `global.dnsService` to your cluster DNS Service (`kubectl get svc -n kube-system`). **RKE2:** `rke2-coredns-rke2-coredns` (see [values-mimir.yaml](../deployment/k8s/observability/helm/values-mimir.yaml)). Then `helm upgrade mimir ...` |
| Loki install: `more than zero replicas` for single binary and scalable | In [values-loki.yaml](../deployment/k8s/observability/helm/values-loki.yaml) set `write`/`read`/`backend` `replicas: 0` with `singleBinary.replicas: 1` |
| Tempo: `Access Key Id ... does not exist` | Chart must set `tempo.extraArgs.config.expand-env: "true"` and `tempo.extraEnv` from `minio-credentials` ([values-tempo.yaml](../deployment/k8s/observability/helm/values-tempo.yaml)); Secret must match running MinIO |
| Tempo/Grafana deprecated chart WARN | Use `grafana-community/tempo` / `grafana-community/grafana`; uninstall old release if upgrade fails |
| Alloy: `otelcol.exporter.debug` experimental | Phase 5 uses wired exporters in [values-alloy-gateway.yaml](../deployment/k8s/observability/helm/values-alloy-gateway.yaml); do not use debug exporter with `stabilityLevel: generally-available` |
| Alloy gateway CrashLoop after Phase 5 | `kubectl logs -n observability deploy/alloy-gateway` — fix River syntax; validate against [config.alloy](../../observability/alloy/config.alloy) |
| No traces in Tempo | Apps `OTEL_EXPORTER_OTLP_ENDPOINT` → `alloy-gateway:4317`; gateway pods Running; generate traffic to web + CMS |
| Faro empty | Browser POST `/api/faro/collect` → 202; Loki `{service_name="unknown_service"} \|= "faro-web"` (not `demo-umbr-fe`); no Faro datasource in Grafana |
| Tempo: only `/api/health` for web | Exclude probes: `span.http.route != "/api/health"`; use `name = "cms.fetch"` |
| Tempo: only SQL for cms | Filter HTTP: `span.http.route != ""`; open full trace |
| Mimir: web missing | Use `http_server_duration_milliseconds_*` and `job="demo-services/umbraco-web"` |
| Mimir/Loki CrashLoop | MinIO buckets exist (`job/minio-create-buckets`); secret keys match values files |
| Datasource error in Grafana | `kubectl -n observability get svc` — gateway service names match [values-grafana.yaml](../deployment/k8s/observability/helm/values-grafana.yaml) |
| Alloy gateway no OTLP | `kubectl -n observability get svc alloy-gateway` ports 4317/4318; apply [gateway-service.yaml](../deployment/k8s/observability/alloy/gateway-service.yaml) |
| Grafana 404 via ingress | DNS `demo-grafana.1node.tech` → nginx → Traefik; IngressRoute applied in `observability` namespace |
| OpenMaxIO blank / login fail | `CONSOLE_MINIO_SERVER` must reach `minio:9000`; check `openmaxio-console` secret; use MinIO root credentials |

See also [MONITORING_STACK.md](./MONITORING_STACK.md).
